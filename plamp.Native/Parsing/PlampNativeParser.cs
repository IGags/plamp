using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.ControlFlow;
using plamp.Ast.Node.Unary;
using plamp.Native.Parsing.Transactions;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Enumerations;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Parsing;

public sealed class PlampNativeParser
{
    internal delegate ExpressionParsingResult TryParseInternal<T>(out T result);
    
    private TokenSequence _tokenSequence;
    private DepthCounter _depth;
    private ParsingTransactionSource _transactionSource;
    
    [Obsolete("For test purposes only")]
    internal TokenSequence TokenSequence => _tokenSequence;

    [Obsolete("For test purposes only")]
    internal PlampNativeParser(string code)
    {
        var tokenRes = code.Tokenize();
        if (tokenRes.Exceptions.Any())
        {
            throw new Exception("Invalid token sequence");
        }   
        _depth = 0;
        _tokenSequence = tokenRes.Sequence;
    }

    public PlampNativeParser(){}
    
    public ParserResult Parse(string code)
    {
        var tokenRes = code.Tokenize();

        _depth = 0;
        _transactionSource = new ParsingTransactionSource(tokenRes.Sequence, tokenRes.Exceptions);
        _tokenSequence = tokenRes.Sequence;

        var expressionList = new List<NodeBase>();

        while (_tokenSequence.Current() != null || _tokenSequence.PeekNext() != null)
        {
            TryParseTopLevel(out var node);
            if (node != null)
            {
                expressionList.Add(node);
            }
        }

        return new ParserResult(expressionList, tokenRes.Exceptions);
    }
    
    internal bool TryParseTopLevel(out NodeBase resultNode)
    {
        resultNode = null;
        if (_tokenSequence.PeekNext() == null)
        {
            return false;
        }
        var currentStart = _tokenSequence.CurrentStart;
        
        if (TryParseScopedWithDepth<EmptyNode>(TryParseEmpty, out var empty, isStrict: true))
        {
            resultNode = empty;
            return true;
        }

        var handleList = new List<DepthHandle>();
        while (_tokenSequence.PeekNext()?.GetType() == typeof(Scope))
        {
            _tokenSequence.GetNextToken();
            handleList.Add(_depth.EnterNewScope());    
        }

        handleList.Reverse();
        
        var token = _tokenSequence.PeekNextNonWhiteSpace();
        if (token is Word word)
        {
            switch (word.ToKeyword())
            {
                case Keywords.Def:
                    var defRes = TryParseFunction(out var defNode);
                    resultNode = defNode;
                    DisposeHandles();
                    return defRes;
                case Keywords.Use:
                    var useRes = TryParseUsing(out var useNode);
                    resultNode = useNode;
                    DisposeHandles();
                    return useRes;
                default:
                    _tokenSequence.GetNextToken();
                    //TODO : Нормальный код
                    AdvanceToEndOfLineAndAddException();
                    return false;
            }
        }
//TODO : Нормальный код
        return false;

        void DisposeHandles()
        {
            foreach (var handle in handleList)
            {
                handle.Dispose();
            }
        }
    }

    internal bool TryParseEmpty(out EmptyNode node)
    {
        if (TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, () => {}, out _))
        {
            node = new EmptyNode();
            return true;
        }

        node = null;
        return false;
    }

    internal bool TryParseUsing(out UseNode node)
    {
        node = null;
        if (!TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Use, () => { }, out _))
        {
            return false;
        }
        
        var use = ParseDotSeparatedName(ParserErrorConstants.InvalidAssemblyName, true);
        if (use == null)
        {
            AdvanceToEndOfLineAndAddException();
            return false;
        }

        node = new UseNode(new MemberNode(use));
        AdvanceToEndOfLineAndAddException();
        return true;
    }

    internal bool TryParseFunction(out DefNode node)
    {
        node = null;
        if (_tokenSequence.PeekNextNonWhiteSpace() == null 
            || !TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Def, () => { }, out _))
        {
            return false;
        }
        
        TryParseType(out var typeNode, true);
        var next = _tokenSequence.PeekNextNonWhiteSpace();
        Word word = null;
        if (next == null || next.GetType() != typeof(Word))
        {
            AddNextTokenException(() => _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedFunctionName,
                _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd)));
        }
        else
        {
            TryConsumeNextNonWhiteSpace(x => x.ToKeyword() == Keywords.Unknown, () => AddNextTokenException(AddKeywordException), out word);
        }
        
        TryParseInParen<List<ParameterNode>, OpenParen, CloseParen>(
            WrapParseCommaSeparated<ParameterNode>(TryParseParameter),
            () => [], out var parameterNodes);
        AdvanceToEndOfLineAndAddException();
        TryParseBody(out var body);
        node = new DefNode(typeNode, word == null ? null : new MemberNode(word.GetStringRepresentation()), parameterNodes, body);
        return true;
    }

    internal bool TryParseBody(out BodyNode body)
    {
        body = null;
        using var handle = _depth.EnterNewScope();
        var expressions = new List<NodeBase>();
        while (TryParseScopedWithDepth<NodeBase>(TryParseBodyLevelExpression, out var expression))
        {
            expressions.Add(expression);
            AdvanceToEndOfLineAndAddException();
        }

        body = new BodyNode(expressions);
        return true;
    }
    
    internal bool TryParseParameter(out ParameterNode parameterNode)
    {
        parameterNode = null;
        var peek = _tokenSequence.PeekNextNonWhiteSpace();
        if (peek == null || peek.GetType() != typeof(Word))
        {
            return false;
        }
        
        TryParseType(out var type, true);
        peek = _tokenSequence.PeekNextNonWhiteSpace();
        if (peek is null || peek.GetType() != typeof(Word))
        {
            AddNextTokenException(() => _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedParameterName, _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd)));
            parameterNode = new ParameterNode(type, null);
            return true;
        }

        if (!TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Unknown,
                () => AddNextTokenException(AddKeywordException), out var word))
        {
            _tokenSequence.GetNextNonWhiteSpace();
        }

        var name = word == null ? null : new MemberNode(word.GetStringRepresentation());
        parameterNode = new ParameterNode(type, name);
        return true; 
    }
    
    internal ExpressionParsingResult TryParseType(out TypeNode typeNode)
    {
        typeNode = null;
        
        var peek = _tokenSequence.PeekNextNonWhiteSpace();
        if (peek == null || peek.GetType() != typeof(Word))
        {
            return false;
        }

        var last = ParseDotSeparatedName(ParserErrorConstants.InvalidTypeName, isAddException);

        if (last == null)
        {
            return false;
        }
        
        var typeName = new MemberNode(last);
        var next = _tokenSequence.PeekNextNonWhiteSpace();
        if (next is not OpenAngleBracket)
        {
            typeNode = new TypeNode(typeName, []);
            return true;
        }

        var genericStart = new TokenPosition(_tokenSequence.CurrentEnd.Pos + 1);
        TryParseInParen<List<TypeNode>, OpenAngleBracket, CloseAngleBracket>(
            WrapParseCommaSeparated<TypeNode>(TryParseTypeWrapper),
            () =>
            {
                if (isAddException)
                {
                    _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedInnerGenerics, genericStart,
                        _tokenSequence.CurrentEnd));
                }
                return [];
            }, out var list);
        typeNode = new TypeNode(typeName, list);
        return true;

        bool TryParseTypeWrapper(out TypeNode type)
        {
            var res = TryParseType(out type, isAddException);
            if (isAddException && !res)
            {
                _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedType, _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd));
            }

            return res;
        }
    }

    internal bool TryParseScopedWithDepth<TReturn>(TryParseInternal<TReturn> @internal, out TReturn result,
        int depth = -1, bool isStrict = false)
    {
        var position = _tokenSequence.Position;
        if (depth < 0)
        {
            depth = (int)_depth;
        }

        var currentDepth = 0;
        while (TryConsumeNextNonWhiteSpace<Scope>(_ => true, () => { }, out _))
        {
            currentDepth++;
        }

        if (currentDepth < depth)
        {
            _tokenSequence.Position = position;
            result = default;
            return false;
        }

        var res = @internal(out result);
        
        if (!res && isStrict)
        {
            _tokenSequence.Position = position;
            result = default;
            return false;
        }
        
        return true;
    }

    internal bool TryParseBodyLevelExpression(out NodeBase expression)
    {
        expression = null;
        if (_tokenSequence.PeekNextNonWhiteSpace() is null)
        {
            return false;
        }
        
        if (TryParseEmpty(out var emptyNode))
        {
            expression = emptyNode;
            return true;
        }

        if (TryParseKeywordExpression(out var keywordExpression))
        {
            expression = keywordExpression;
            AdvanceToEndOfLineAndAddException();
            return true;
        }

        var position = _tokenSequence.Position;
        if (TryParseWithPrecedence(out expression))
        {
            AdvanceToEndOfLineAndAddException();
            return true;
        }
        _tokenSequence.Position = position;

        var startPos = new TokenPosition(_tokenSequence.CurrentEnd.Pos + 1);
        AdvanceToFirstOfTokens([typeof(EndOfLine)]);
        _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedBodyLevelExpression, startPos, _tokenSequence.CurrentEnd));
        return false;
    }

    internal bool TryParseKeywordExpression(out NodeBase expression)
    {
        expression = null;
        var position = _tokenSequence.Position;
        if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() != Keywords.Unknown, () => { }, out var keyword))
        {
            switch (keyword.ToKeyword())
            {
                case Keywords.Break:
                    expression = new BreakNode();
                    AdvanceToEndOfLineAndAddException();
                    return true;
                case Keywords.Continue:
                    expression = new ContinueNode();
                    AdvanceToEndOfLineAndAddException();
                    return true;
                case Keywords.Return:
                    TryParseWithPrecedence(out var precedence);
                    expression = new ReturnNode(precedence);
                    AdvanceToEndOfLineAndAddException();
                    return true;
                case Keywords.If:
                    _tokenSequence.RollBackToNonWhiteSpace();
                    TryParseConditionalExpression(out var node);
                    AdvanceToEndOfLineAndAddException();
                    expression = node;
                    return true;
                case Keywords.For:
                    _tokenSequence.RollBackToNonWhiteSpace();
                    TryParseForLoop(out var forNode);
                    AdvanceToEndOfLineAndAddException();
                    expression = forNode;
                    return true;
                case Keywords.While:
                    _tokenSequence.RollBackToNonWhiteSpace();
                    TryParseWhileLoop(out var whileNode);
                    AdvanceToEndOfLineAndAddException();
                    expression = whileNode;
                    return true;
                default:
                    _tokenSequence.Position = position;
                    return false;
            }
        }
        return false;
    }

    internal ExpressionParsingResult TryParseConditionalExpression(IParsingTransaction transaction, out ConditionNode conditionNode)
    {
        conditionNode = null;
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.If, _ => { }, out var ifToken))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }

        TryParseConditionClause(transaction, out var baseClause);
        
        var elifClauses = new List<ClauseNode>();

        var lastPos = _tokenSequence.Position;
        while (TryParseEmpty(out _) || TryParseScopedWithDepth<Word>(TryParseElifKeyword, out _, isStrict:true))
        {
            TryParseConditionClause(out var elifClause);
            elifClauses.Add(elifClause);

            lastPos = _tokenSequence.Position;
        }

        _tokenSequence.Position = lastPos;
        var elseBody = default(BodyNode);
        if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Else, () => {}, out _))
        {
            AdvanceToEndOfLineAndAddException();
            TryParseBody(out elseBody);
        }
            
        conditionNode = new ConditionNode(baseClause, elifClauses, elseBody);
        return true;

        return false;
        bool TryParseElifKeyword(out Word res)
        {
             return TryConsumeNextNonWhiteSpace(x => x.ToKeyword() == Keywords.Elif, () => { }, out res);
        }
    }

    internal ExpressionParsingResult TryParseConditionClause(IParsingTransaction transaction, out ClauseNode conditionNode)
    {

        var res = TryParseInParen<NodeBase, OpenParen, CloseParen>(
            transaction, TryParseWithPrecedence, (_, _) => default, out var condition,
            ExpressionParsingResult.FailedNeedRollback, ExpressionParsingResult.Success);
        
        var body = ParseOptionalBody(transaction);
        conditionNode = new ClauseNode(condition, body);
        return res;
    }

    internal ExpressionParsingResult TryParseForLoop(IParsingTransaction transaction, out ForNode forNode)
    {
        forNode = null;
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.For, _ => { }, out var keyword))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }

        TryParseInParen<ForHeaderHolder, OpenParen, CloseParen>(
            transaction, ForHeaderWrapper, (_, _) => default, out var holder,
            ExpressionParsingResult.FailedNeedRollback, ExpressionParsingResult.FailedNeedCommit);
        var body = ParseOptionalBody(transaction);
        
        forNode = new ForNode(holder.IteratorVar, holder.Iterable, body);
        return ExpressionParsingResult.Success;

        ExpressionParsingResult ForHeaderWrapper(out ForHeaderHolder header) =>
            TryParseForHeader(transaction, keyword, out header);
    }

    internal record struct ForHeaderHolder(NodeBase IteratorVar, NodeBase Iterable);

    internal ExpressionParsingResult TryParseForHeader(IParsingTransaction transaction, TokenBase forNode, out ForHeaderHolder headerHolder)
    {
        TryParseWithPrecedence(out var iteratorVar);
        TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.In, 
            _ => transaction.AddException(new PlampException(PlampNativeExceptionInfo.InvalidForHeader(), forNode)), out _);
        TryParseWithPrecedence(out var iterable);
        headerHolder = new ForHeaderHolder(iteratorVar, iterable);
        return ExpressionParsingResult.Success;
    }
    
    internal ExpressionParsingResult TryParseWhileLoop(IParsingTransaction transaction, out WhileNode whileNode)
    {
        whileNode = null;

        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.While, _ => { }, out _))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }

        var res = TryParseInParen<NodeBase, OpenParen, CloseParen>(
            transaction, TryParseWithPrecedence, (_, _) => null, out var expression, 
            ExpressionParsingResult.FailedNeedCommit, ExpressionParsingResult.Success);

        var body = ParseOptionalBody(transaction);
        
        switch (res)
        {
            case ExpressionParsingResult.Success:
                whileNode = new WhileNode(expression, body);
                return ExpressionParsingResult.Success;
            case ExpressionParsingResult.FailedNeedCommit:
                whileNode = new WhileNode(null, body);
                return ExpressionParsingResult.Success;
        }

        throw new Exception("Parser exception");
    }

    internal BodyNode ParseOptionalBody(IParsingTransaction transaction)
    {
        if (_tokenSequence.PeekNext()?.GetType() != typeof(EndOfLine))
        {
            TryParseWithPrecedence(out var expression);
            if (!TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, _ => { }, out _))
            {
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
            }
            return new BodyNode([expression]);
        }

        if (!TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, _ => { }, out _))
        {
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
        }

        TryParseBody(out var body);
        return body;
    }

    #region ExpressionParsing

    internal enum ExpressionParsingResult
    {
        Success,
        FailedNeedRollback,
        FailedNeedCommit,
        FailedNeedPass,
    }

    internal ExpressionParsingResult TryParseWithPrecedence(out NodeBase node) 
        => TryParseWithPrecedence(out node, 0);
    
    internal ExpressionParsingResult TryParseWithPrecedence(out NodeBase node, int rbp = 0)
    {
        var nudParsingResult = TryParseNud(out node);
        if (nudParsingResult != ExpressionParsingResult.Success)
        {
            return nudParsingResult;
        }

        while (TryParseLed(rbp, node, out node) == ExpressionParsingResult.Success)
        {
        }

        return ExpressionParsingResult.Success;
    }
    
    internal ExpressionParsingResult TryParseNud(out NodeBase node)
    {
        var transaction = _transactionSource.BeginTransaction();
        var result = TryParseVariableDeclaration(transaction, out node);
        switch (result)
        {
            case ExpressionParsingResult.Success:
                transaction.Commit();
                return ExpressionParsingResult.Success;
            case ExpressionParsingResult.FailedNeedCommit:
                transaction.Commit();
                return ExpressionParsingResult.FailedNeedCommit;
            case ExpressionParsingResult.FailedNeedPass:
            default:
                transaction.Pass();
                break;
        }

        transaction = _transactionSource.BeginTransaction();
        switch (TryParseCastOperator(transaction, out var typeCasting))
        {
            case ExpressionParsingResult.Success:
                if (TryParseNud(out var cast) == ExpressionParsingResult.Success)
                {
                    node = new CastNode(typeCasting, cast);
                    transaction.Commit();
                    return ExpressionParsingResult.Success;
                }
                transaction.Commit();
                return ExpressionParsingResult.FailedNeedCommit;
            case ExpressionParsingResult.FailedNeedRollback:
                transaction.Rollback();
                break;
            case ExpressionParsingResult.FailedNeedCommit:
                transaction.Commit();
                return ExpressionParsingResult.FailedNeedCommit;
        }

        transaction = _transactionSource.BeginTransaction();
        switch (TryParseSubExpression(transaction, out var sub))
        {
            case ExpressionParsingResult.Success:
                node = sub;
                node = ParsePostfixIfExist(sub);
                return ExpressionParsingResult.Success;
            case ExpressionParsingResult.FailedNeedCommit:
                transaction.Commit();
                return ExpressionParsingResult.FailedNeedCommit;
            case ExpressionParsingResult.FailedNeedRollback:
            default:
                transaction.Rollback();
                break;
        }

        transaction = _transactionSource.BeginTransaction();
        var ctorParseRes = TryParseConstructor(transaction, out var ctor);
        switch (ctorParseRes)
        {
            case ExpressionParsingResult.Success:
                node = ctor;
                transaction.Commit();
                return ExpressionParsingResult.Success;
            case ExpressionParsingResult.FailedNeedCommit:
                transaction.Commit();
                return ExpressionParsingResult.FailedNeedCommit;
            case ExpressionParsingResult.FailedNeedRollback:
            default:
                transaction.Rollback();
                break;
        }

        if (TryParsePrefixOperator(out node) == ExpressionParsingResult.Success)
        {
            return ExpressionParsingResult.Success;
        }

        if (TryParseLiteral(out node) == ExpressionParsingResult.Success)
        {
            return ExpressionParsingResult.Success;
        }

        if (TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out var word))
        {
            var member = new MemberNode(word.GetStringRepresentation());
            node = ParsePostfixIfExist(member);
            return ExpressionParsingResult.Success;
        }

        node = null;
        transaction.Rollback();
        //TODO
        return ExpressionParsingResult.FailedNeedCommit;
    }
    
    internal ExpressionParsingResult TryParseLed(int rbp, NodeBase left, out NodeBase output)
    {
        var transaction = _transactionSource.BeginTransaction();
        SkipLineBreak();
        
        if (TryConsumeNextNonWhiteSpace<OperatorToken>(_ => true, op => { }, out var token))
        {
            var precedence = token.GetPrecedence(false);
            if (precedence <= rbp)
            {
                output = left;
                _tokenSequence.RollBackToNonWhiteSpace();
                return ExpressionParsingResult.FailedNeedCommit;
            }

            var res = TryParseWithPrecedence(out var right, precedence);
            if (res == ExpressionParsingResult.Success && right != null)
            {
                switch (token.Operator)
                {
                    case OperatorEnum.Multiply:
                        output = new MultiplyNode(left, right);
                        break;
                    case OperatorEnum.Divide:
                        output = new DivideNode(left, right);
                        break;
                    case OperatorEnum.Plus:
                        output = new PlusNode(left, right);
                        break;
                    case OperatorEnum.Minus:
                        output = new MinusNode(left, right);
                        break;
                    case OperatorEnum.Lesser:
                        output = new LessNode(left, right);
                        break;
                    case OperatorEnum.Greater:
                        output = new GreaterNode(left, right);
                        break;
                    case OperatorEnum.LesserOrEquals:
                        output = new LessOrEqualNode(left, right);
                        break;
                    case OperatorEnum.GreaterOrEquals:
                        output = new GreaterOrEqualsNode(left, right);
                        break;
                    case OperatorEnum.Equals:
                        output = new EqualNode(left, right);
                        break;
                    case OperatorEnum.NotEquals:
                        output = new NotEqualNode(left, right);
                        break;
                    case OperatorEnum.And:
                        output = new AndNode(left, right);
                        break;
                    case OperatorEnum.Or:
                        output = new OrNode(left, right);
                        break;
                    case OperatorEnum.Modulo:
                        output = new ModuloNode(left, right);
                        break;
                    case OperatorEnum.Assign:
                        output = new AssignNode(left, right);
                        break;
                    case OperatorEnum.PlusAndAssign:
                        output = new AddAndAssignNode(left, right);
                        break;
                    case OperatorEnum.MinusAndAssign:
                        output = new SubAndAssignNode(left, right);
                        break;
                    case OperatorEnum.MultiplyAndAssign:
                        output = new MulAndAssignNode(left, right);
                        break;
                    case OperatorEnum.DivideAndAssign:
                        output = new DivAndAssignNode(left, right);
                        break;
                    case OperatorEnum.ModuloAndAssign:
                        output = new ModuloAndAssignNode(left, right);
                        break;
                    case OperatorEnum.AndAndAssign:
                        output = new AndAndAssignNode(left, right);
                        break;
                    case OperatorEnum.OrAndAssign:
                        output = new OrAndAssignNode(left, right);
                        break;
                    case OperatorEnum.XorAndAssign:
                        output = new XorAndAssignNode(left, right);
                        break;
                    case OperatorEnum.BitwiseAnd:
                        output = new BitwiseAndNode(left, right);
                        break;
                    case OperatorEnum.BitwiseOr:
                        output = new BitwiseOrNode(left, right);
                        break;
                    case OperatorEnum.Xor:
                        output = new XorNode(left, right);
                        break;
                    //TODO: 2 инкремента проверить, как будет себя вести
                    default:
                        throw new Exception();
                }
                transaction.Commit();
                return ExpressionParsingResult.Success;
            }
            
            output = left;
            transaction.Rollback();
            return ExpressionParsingResult.FailedNeedCommit;
        }
        
        transaction.Rollback();
        output = left;
        return ExpressionParsingResult.FailedNeedCommit;
    }
    
    internal ExpressionParsingResult TryParseVariableDeclaration(IParsingTransaction transaction, out NodeBase variableDeclaration)
    {
        var typ = default(TypeNode);
        if (TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.Var, _ => { }, out _) 
            || TryParseType(out typ) == ExpressionParsingResult.Success)
        {
            //Null denotation starts with variable declaration
            if (TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(
                    _ => true,
                    token => transaction.AddException(new PlampException(PlampNativeExceptionInfo.ExpectedIdentifier(), token)), 
                    out var name))
            {
                variableDeclaration = new VariableDefinitionNode(typ, new MemberNode(name.GetStringRepresentation()));
                return ExpressionParsingResult.Success;
            }
            variableDeclaration = null;
            return ExpressionParsingResult.FailedNeedCommit;
        }

        variableDeclaration = null;
        return ExpressionParsingResult.FailedNeedRollback;
    }

    internal ExpressionParsingResult TryParseCastOperator(IParsingTransaction transaction, out TypeNode cast)
    {
        cast = null;
        return TryParseInParen<TypeNode, OpenParen, CloseParen>(
            transaction, TryParseType,
            (open, close) =>
            {
                AddExceptionToTheTokenRange(open, close, PlampNativeExceptionInfo.InvalidCastOperator(),
                    transaction);
                return null;
            }, out cast,
            ExpressionParsingResult.FailedNeedRollback, ExpressionParsingResult.FailedNeedCommit);
    }

    internal ExpressionParsingResult TryParseSubExpression(IParsingTransaction transaction, out NodeBase sub)
    {
        return TryParseInParen<NodeBase, OpenParen, CloseParen>(
            transaction, TryParseWithPrecedence, (open, close) =>
            {
                AddExceptionToTheTokenRange(open, close, PlampNativeExceptionInfo.ExpectedExpression(),
                    transaction);
                return null;
            },
            out sub, 
            ExpressionParsingResult.FailedNeedRollback, ExpressionParsingResult.FailedNeedCommit);
    }

    internal ExpressionParsingResult TryParseLiteral(out NodeBase node)
    {
        node = null;
        if (TryConsumeNextNonWhiteSpace<StringLiteral>(_ => true, _ => { }, out var literal))
        {
            var stringLiteral = new ConstNode(literal.GetStringRepresentation(), typeof(string));
            node = ParsePostfixIfExist(stringLiteral);
            return ExpressionParsingResult.Success;
        }

        if (TryConsumeNextNonWhiteSpace<NumberLiteral>(_ => true, _ => { }, out var numberLiteral))
        {
            var number = new ConstNode(numberLiteral.ActualValue, numberLiteral.ActualType);
            node = ParsePostfixIfExist(number);
            return ExpressionParsingResult.Success;
        }

        if (TryConsumeNextNonWhiteSpace(t => t.Keyword is Keywords.True or Keywords.False, _ => { },
                out KeywordToken boolLiteral))
        {
            var value = bool.Parse(boolLiteral.GetStringRepresentation());
            var boolNode = new ConstNode(value, typeof(bool));
            node = ParsePostfixIfExist(boolNode);
            return ExpressionParsingResult.Success;
        }

        if (TryConsumeNextNonWhiteSpace(t => t.Keyword is Keywords.Null, _ => { }, out KeywordToken _))
        {
            var nullNode = new ConstNode(null, null);
            node = ParsePostfixIfExist(nullNode);
            return ExpressionParsingResult.Success;
        }

        return ExpressionParsingResult.FailedNeedRollback;
    }

    private ExpressionParsingResult TryParseConstructor(IParsingTransaction transaction, out NodeBase ctor)
    {
        ctor = null;
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.New, _ => { }, out var keywordToken))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        if (TryParseType(out var type) != ExpressionParsingResult.Success)
        {
            return ExpressionParsingResult.FailedNeedCommit;
        }
        
        var typeEnd = _tokenSequence.Current();
        var parenRes = TryParseInParen<List<NodeBase>, OpenParen, CloseParen>(
            transaction,
            WrapParseCommaSeparated<NodeBase>(TryParseWithPrecedence, ExpressionParsingResult.FailedNeedCommit),
            (_, _) => [], out var parameters, ExpressionParsingResult.FailedNeedCommit,
            ExpressionParsingResult.Success);
        
        switch (parenRes)
        {
            case ExpressionParsingResult.Success:
                ctor = new ConstructorNode(type, parameters);
                return ExpressionParsingResult.Success;
            case ExpressionParsingResult.FailedNeedCommit:
                AddExceptionToTheTokenRange(keywordToken, typeEnd,
                    PlampNativeExceptionInfo.Expected("arguments in ()"), transaction);
                return ExpressionParsingResult.FailedNeedCommit;
        }

        return ExpressionParsingResult.FailedNeedCommit;
    }

    internal ExpressionParsingResult TryParsePrefixOperator(out NodeBase node)
    {
        node = null;
        if (TryConsumeNextNonWhiteSpace<OperatorToken>(
                x => x.Operator is OperatorEnum.Minus or OperatorEnum.Not or OperatorEnum.Increment or OperatorEnum.Decrement,
                _ => { }, out var operatorToken))
        {
            TryParseWithPrecedence(out var inner, operatorToken.GetPrecedence(true));

            node = operatorToken.Operator switch
            {
                OperatorEnum.Minus => new UnaryMinusNode(inner),
                OperatorEnum.Not => new NotNode(inner),
                OperatorEnum.Increment => new PrefixIncrementNode(inner),
                OperatorEnum.Decrement => new PrefixDecrementNode(inner)
            };

            node = ParsePostfixIfExist(node);
            return ExpressionParsingResult.Success;
        }

        return ExpressionParsingResult.FailedNeedRollback;
    }
    
    internal NodeBase ParsePostfixIfExist(NodeBase inner)
    {
        while (true)
        {
            if (TryParsePostfixOperator(inner, out var node))
            {
                inner = node;
                continue;
            }

            if (TryParseIndexer(inner, out node))
            {
                inner = node;
                continue;
            }

            if (TryParseCall(inner, out node))
            {
                inner = node;
                continue;
            }
            
            if (!TryParseMemberAccess(inner, out node)) return inner;
            inner = node;
        }
    }
    
    internal bool TryParsePostfixOperator(NodeBase nodeBase, out NodeBase node)
    {
        node = null;
        if (!TryConsumeNextNonWhiteSpace<OperatorToken>(
                x => x is { Operator: OperatorEnum.Increment or OperatorEnum.Decrement },
                _ => { }, out var @operator))
        {
            return false;
        }
        
        node =  @operator.Operator switch
        {
            OperatorEnum.Increment => new PostfixIncrementNode(nodeBase),
            OperatorEnum.Decrement => new PostfixDecrementNode(nodeBase),
            _ => throw new Exception("Parser exception")
        };
        return true;

    }
    
    internal bool TryParseIndexer(NodeBase inner, out NodeBase node)
    {
        var next = _tokenSequence.PeekNextNonWhiteSpace();
        if (next is not OpenSquareBracket)
        {
            node = null;
            return false;
        }

        var transaction = _transactionSource.BeginTransaction();
        var isParsed = TryParseInParen<List<NodeBase>, OpenSquareBracket, CloseSquareBracket>(
            transaction, 
            WrapParseCommaSeparated<NodeBase>(TryParseWithPrecedence, ExpressionParsingResult.FailedNeedCommit), (_, _) => [], 
            out var index, ExpressionParsingResult.FailedNeedRollback, ExpressionParsingResult.Success);
        if (isParsed == ExpressionParsingResult.Success)
        {
            node = new IndexerNode(inner, index);
            return true;
        }

        node = null;
        return false;
    }
    
    internal bool TryParseMemberAccess(NodeBase input, out NodeBase res)
    {
        res = null;
        if (TryConsumeNextNonWhiteSpace<OperatorToken>(x => x.Operator == OperatorEnum.MemberAccess, _ => { }, out var call))
        {
            if (TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out var word))
            {
                res = new MemberAccessNode(input, new MemberNode(word.GetStringRepresentation()));
                return true;
            }

            var transaction = _transactionSource.BeginTransaction();
            transaction.AddException(new PlampException(PlampNativeExceptionInfo.ExpectedMemberName(), call));
            transaction.Commit();
            return false;
        }

        res = null;
        return false;
    }

    internal bool TryParseCall(NodeBase input, out NodeBase res)
    {
        res = null;
        var transaction = _transactionSource.BeginTransaction();
        var parenRes = TryParseInParen<List<NodeBase>, OpenParen, CloseParen>(
            transaction,
            WrapParseCommaSeparated<NodeBase>(TryParseWithPrecedence, ExpressionParsingResult.FailedNeedPass),
            (_, _) => [], out var args, ExpressionParsingResult.FailedNeedRollback, ExpressionParsingResult.Success);
        switch (parenRes)
        {
            case ExpressionParsingResult.Success:
                res = new CallNode(input, args);
                return true;
            case ExpressionParsingResult.FailedNeedRollback:
                transaction.Rollback();
                return false;
            case ExpressionParsingResult.FailedNeedCommit:
                transaction.Commit();
                return false;
            default:
                transaction.Pass();
                return false;
        }
    }

    #endregion
    
    #region Helper

    internal void RollBackToRequestedNonWhiteSpaceToken<T>() where T : TokenBase
    {
        if (_tokenSequence.Current() != null && _tokenSequence.Current().GetType() == typeof(T))
        {
            return;
        }
        
        while (true)
        {
            var token = _tokenSequence.RollBackToNonWhiteSpace();
            if (token is null || token.GetType() == typeof(T))
            {
                return;
            }
        }
    }

    internal void AdvanceToRequestedToken<T>() where T : TokenBase
    {
        if (_tokenSequence.Current() != null && _tokenSequence.Current().GetType() == typeof(T))
        {
            return;
        }
        
        while (true)
        {
            var token = _tokenSequence.PeekNext();
            if (token == null)
            {
                _tokenSequence.GetNextToken();
                return;
            }
            if (token.GetType() == typeof(T))
            {
                _tokenSequence.GetNextToken();
                return;
            }
            _tokenSequence.GetNextToken();
        }
    }

    internal void AdvanceToFirstOfTokens(List<Type> tokenTypes)
    {
        if (_tokenSequence.Current() != null && tokenTypes.Contains(_tokenSequence.Current().GetType()))
        {
            return;
        }
        
        while (true)
        {
            var token = _tokenSequence.PeekNext();
            if (token == null)
            {
                _tokenSequence.GetNextToken();
                return;
            }

            if (tokenTypes.Contains(token.GetType()))
            {
                _tokenSequence.GetNextToken();
                return;
            }

            _tokenSequence.GetNextToken();
        }
    }
    
    internal void AddKeywordException() =>
        _exceptions.Add(new ParserException(ParserErrorConstants.CannotUseKeyword, _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd));

    
    
    internal void AddUnexpectedToken<T>() where T : TokenBase =>
        _exceptions.Add(new ParserException($"{ParserErrorConstants.UnexpectedTokenPrefix} {typeof(T).Name}", _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd));

    
    
    
    #endregion

    #region Helper Clear

    internal ExpressionParsingResult TryParseCommaSeparated<TReturn>(TryParseInternal<TReturn> parserFunc, out List<TReturn> result, ExpressionParsingResult resultIfFail)
    {
        result = [];
        var accumulate = ExpressionParsingResult.Success;
        while (true)
        {
            if (parserFunc(out var res) != ExpressionParsingResult.Success)
            {
                accumulate = resultIfFail;
            }
            
            result.Add(res);
            
            if (!TryConsumeNextNonWhiteSpace<Comma>(_ => true, _ => {}, out _))
            {
                return accumulate;
            }
        }
    }
    
    internal bool TryConsumeNextNonWhiteSpaceWithoutRollback<TToken>(Func<TToken, bool> predicate, 
        Action<TokenBase> ifPredicateFalse, out TToken token) where TToken : TokenBase
    {
        token = null;
        if (typeof(TToken) == typeof(WhiteSpace))
        {
            return false;
        }
        
        var next = _tokenSequence.GetNextNonWhiteSpace();
        if (next is TToken target && predicate(target))
        {
            token = target;
            return true;
        }

        ifPredicateFalse(next);
        return false;
    }
    
    internal bool TryConsumeNextNonWhiteSpace<TToken>(Func<TToken, bool> predicate, Action<TokenBase> ifPredicateFalse, out TToken token)
        where TToken : TokenBase
    {
        token = null;
        if (typeof(TToken) == typeof(WhiteSpace))
        {
            return false;
        }
        var next = _tokenSequence.PeekNextNonWhiteSpace();
        if (next is TToken target && predicate(target))
        {
            token = target;
            _tokenSequence.GetNextNonWhiteSpace();
            return true;
        }

        ifPredicateFalse(next);
        return false;
    }
    
    internal ExpressionParsingResult TryParseInParen<TResult, TOpen, TClose>(
        IParsingTransaction transaction,
        TryParseInternal<TResult> parserFunc, 
        Func<TokenBase, TokenBase, TResult> emptyCase, 
        out TResult result, 
        ExpressionParsingResult missingOpenParen,
        ExpressionParsingResult emptyCaseResult)
        where TOpen : TokenBase where TClose : TokenBase
    {
        result = default;
        
        if (!TryConsumeNextNonWhiteSpace<TOpen>(_ => true, _ => { }, out var open))
        {
            return missingOpenParen;
        }
        if (TryConsumeNextNonWhiteSpace<TClose>(_ => true, t => { }, out var close))
        {
            result = emptyCase(open, close);
            return emptyCaseResult;
        }

        var res = parserFunc(out result);
        var isClosed = TryConsumeNextNonWhiteSpace<TClose>(_ => true,
            _ => {}, out _);

        if (!isClosed) return res;
        AdvanceToRequestedTokenWithException<TClose>(transaction);
        
        return res;
    }
    
    internal void AdvanceToEndOfOrLineRequested<TToken>()
    {
        if (typeof(TToken) == typeof(WhiteSpace))
        {
            throw new Exception("Cannot use with white space");
        }
        
        TokenBase current;
        do
        {
            SkipLineBreak();
            current = _tokenSequence.GetNextNonWhiteSpace();
        } while (current.GetType() != typeof(EndOfLine) && current.GetType() != typeof(TToken));
    }

    private void SkipLineBreak()
    {
        if (TryConsumeNextNonWhiteSpace<LineBreak>(_ => true, _ => { }, out _))
        {
            TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, _ => { }, out _);
        }
    }
    
    private TryParseInternal<List<TReturn>> WrapParseCommaSeparated<TReturn>(
        TryParseInternal<TReturn> parserFunc, ExpressionParsingResult errorResult)
    {
        return FuncWrapper;
        ExpressionParsingResult FuncWrapper(out List<TReturn> resultList)
        {
            return TryParseCommaSeparated(parserFunc, out resultList, errorResult);
        }
    }
    
    #endregion

    #region ExceptionGeneration

    private void AddExceptionToTheTokenRange(TokenBase start, TokenBase end,
        PlampNativeExceptionFinalRecord exceptionRecord, IParsingTransaction transaction)
    {
        transaction.AddException(new PlampException(exceptionRecord, start.Start, end.End));
    }

    private void AdvanceToRequestedTokenWithException<TRequested>(IParsingTransaction transaction)
    {
        var next = _tokenSequence.GetNextNonWhiteSpace();
        AdvanceToEndOfOrLineRequested<TRequested>();
        var end = _tokenSequence.Current();
        AddExceptionToTheTokenRange(next, end, PlampNativeExceptionInfo.Expected(typeof(TRequested).Name), transaction);
    }

    #endregion
}