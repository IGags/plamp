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
    
    internal ExpressionParsingResult TryParseTopLevel(out NodeBase resultNode)
    {
        resultNode = null;
        if (_tokenSequence.PeekNext() == null)
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        var transaction = _transactionSource.BeginTransaction();
        if (TryParseScopedWithDepth<EmptyNode>(TryParseEmpty, out var empty) == ExpressionParsingResult.Success)
        {
            resultNode = empty;
            transaction.Commit();
            return ExpressionParsingResult.Success;
        }
        transaction.Rollback();
        
        var handleList = new List<DepthHandle>();
        while (TryConsumeNext<WhiteSpace>(_ => true, _ => {}, out var space))
        {
            if (space.Kind == WhiteSpaceKind.Scope)
            {
                handleList.Add(_depth.EnterNewScope());
            }
        }

        handleList.Reverse();
        
        var token = _tokenSequence.PeekNextNonWhiteSpace();
        if (token is KeywordToken keyword)
        {
            switch (keyword.Keyword)
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
            }
        }
        
        transaction =_transactionSource.BeginTransaction();
        AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
        transaction.Commit();
        return ExpressionParsingResult.FailedNeedCommit;
        
        void DisposeHandles()
        {
            foreach (var handle in handleList)
            {
                handle.Dispose();
            }
        }
    }

    internal ExpressionParsingResult TryParseEmpty(out EmptyNode node)
    {
        if (TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, _ => {}, out _))
        {
            node = new EmptyNode();
            return ExpressionParsingResult.Success;
        }

        node = null;
        return ExpressionParsingResult.FailedNeedRollback;
    }

    internal ExpressionParsingResult TryParseUsing(out UseNode node)
    {
        node = null;
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.Use, _ => { }, out var use))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }
        var transaction = _transactionSource.BeginTransaction();
        var res = ParseMemberAccessSequence(transaction, out var list);
        if (res == ExpressionParsingResult.FailedNeedRollback)
        {
            transaction.AddException(new PlampException(PlampNativeExceptionInfo.InvalidUsingName(), use));
        }
        list.Reverse();
        
        NodeBase memberNode = new MemberNode(list[0].GetStringRepresentation());
        foreach (var member in list.Skip(1))
        {
            memberNode = new MemberAccessNode(new MemberNode(member.GetStringRepresentation()), memberNode);
        }
        
        AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
        transaction.Commit();
        node = new UseNode(memberNode);
        
        return ExpressionParsingResult.Success;
    }

    internal ExpressionParsingResult TryParseFunction(out DefNode node)
    {
        node = null;
        if (_tokenSequence.PeekNextNonWhiteSpace() == null 
            || !TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.Def, _ => { }, out var def))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }

        var transaction = _transactionSource.BeginTransaction();
        TryParseType(transaction, out var typeNode);
        //TODO: To semantics layer
        MemberNode nameNode;
        if (!TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out var name))
        {
            nameNode = null;
        }
        else
        {
            nameNode = new MemberNode(name.GetStringRepresentation());
        }
        
        var res = TryParseInParen<List<ParameterNode>, OpenParen, CloseParen>(
            transaction,
            WrapParseCommaSeparated<ParameterNode>(ParameterWrapper, ExpressionParsingResult.FailedNeedCommit),
            (_, _) => [], out var parameterNodes, 
            ExpressionParsingResult.FailedNeedPass, ExpressionParsingResult.Success);
        if (res == ExpressionParsingResult.FailedNeedPass)
        {
            transaction.AddException(new PlampException(PlampNativeExceptionInfo.ExpectedArgDefinition(), def));
        }

        var body = ParseOptionalBody(transaction);
        node = new DefNode(typeNode, nameNode, parameterNodes, body);
        return ExpressionParsingResult.Success;

        ExpressionParsingResult ParameterWrapper(out ParameterNode node)
        {
            return TryParseParameter(transaction, out node);
        }
    }

    internal ExpressionParsingResult TryParseBody(out BodyNode body)
    {
        using var handle = _depth.EnterNewScope();
        var expressions = new List<NodeBase>();
        while (true)
        {
            var transaction = _transactionSource.BeginTransaction();
            var res = TryParseScopedWithDepth<NodeBase>(TryParseBodyLevelExpression, out var expression);
            if (res == ExpressionParsingResult.Success)
            {
                expressions.Add(expression);
                continue;
            }
            
            transaction.Rollback();
            break;
        }
        
        body = new BodyNode(expressions);
        return ExpressionParsingResult.Success;
    }
    
    internal ExpressionParsingResult TryParseParameter(IParsingTransaction transaction, out ParameterNode parameterNode)
    {
        parameterNode = null;
        var typePeek = _tokenSequence.PeekNextNonWhiteSpace();
        if (typePeek == null || typePeek.GetType() != typeof(Word))
        {
            return ExpressionParsingResult.FailedNeedPass;
        }
        
        TryParseType(transaction, out var type);
        var argPeek = _tokenSequence.PeekNextNonWhiteSpace();
        MemberNode name;
        if (argPeek is null || argPeek.GetType() != typeof(Word))
        {
            AddExceptionToTheTokenRange(typePeek, argPeek, PlampNativeExceptionInfo.InvalidParameterDefinition(), transaction);
            name = null;
        }
        else
        {
            TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out _);
            name = new MemberNode(argPeek.GetStringRepresentation());
        }
        
        parameterNode = new ParameterNode(type, name);
        return ExpressionParsingResult.Success;
    }
    
    internal ExpressionParsingResult TryParseType(IParsingTransaction transaction, out NodeBase typeNode)
    {
        typeNode = null;
        var res = ParseMemberAccessSequence(transaction, out var list);
        switch (res)
        {
            case ExpressionParsingResult.FailedNeedRollback:
                return ExpressionParsingResult.FailedNeedRollback;
        }

        TryParseInParen<List<NodeBase>, OpenAngleBracket, CloseAngleBracket>(transaction,
            WrapParseCommaSeparated(TryParseTypeWrapper(transaction), ExpressionParsingResult.FailedNeedCommit),
            (start, end)
                =>
            {
                AddExceptionToTheTokenRange(start, end, PlampNativeExceptionInfo.InvalidGenericDefinition(),
                    transaction);
                return null;
            },
            out var types, ExpressionParsingResult.FailedNeedPass, ExpressionParsingResult.Success);

        list.Reverse();
        NodeBase node = new TypeNode(new MemberNode(list[0].GetStringRepresentation()), types);
        foreach (var member in list.Skip(1))
        {
            node = new MemberAccessNode(new MemberNode(member.GetStringRepresentation()), node);
        }
        
        typeNode = node;
        return ExpressionParsingResult.Success;
    }

    internal ExpressionParsingResult ParseMemberAccessSequence(IParsingTransaction transaction, out List<Word> members)
    {
        members = null;
        var peek = _tokenSequence.PeekNextNonWhiteSpace();
        if (peek.GetType() != typeof(Word))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }

        _tokenSequence.GetNextToken();
        members = [(Word)peek];
        
        while (true)
        {
            if (!TryConsumeNextNonWhiteSpace<OperatorToken>(x => x.Operator == OperatorEnum.MemberAccess, 
                    _ => { },
                    out _))
            {
                break;
            }

            var first = members[0];
            if (TryConsumeNextNonWhiteSpace<Word>(_ => true, t =>
                {
                    AddExceptionToTheTokenRange(first, t, PlampNativeExceptionInfo.InvalidTypeName(),
                        transaction);
                }, out var word))
            {
                members.Add(word);
            }
            else
            {
                return ExpressionParsingResult.FailedNeedCommit;
            }
        }

        return ExpressionParsingResult.Success;
    }
    
    internal TryParseInternal<NodeBase> TryParseTypeWrapper(IParsingTransaction transaction)
    {
        return FuncWrapper;
            
        ExpressionParsingResult FuncWrapper(out NodeBase result)
        {
            return TryParseType(transaction, out result);
        }
    }
    
    internal ExpressionParsingResult TryParseScopedWithDepth<TReturn>(
        TryParseInternal<TReturn> @internal, out TReturn result, int depth = -1)
    {
        if (depth < 0)
        {
            depth = (int)_depth;
        }

        var currentDepth = 0;
        while (TryConsumeNext<WhiteSpace>(t =>
               {
                   if (t.Kind == WhiteSpaceKind.Scope)
                   {
                       currentDepth++;
                   }

                   return true;
               }, _ => { }, out _)) { }

        if (currentDepth < depth)
        {
            result = default;
            return ExpressionParsingResult.FailedNeedRollback;
        }

        var res = @internal(out result);

        return res;
    }

    internal ExpressionParsingResult TryParseBodyLevelExpression(out NodeBase expression)
    {
        expression = null;
        if (_tokenSequence.PeekNextNonWhiteSpace() is null)
        {
            return ExpressionParsingResult.FailedNeedCommit;
        }
        
        if (TryParseEmpty(out var emptyNode) == ExpressionParsingResult.Success)
        {
            expression = emptyNode;
            return ExpressionParsingResult.Success;
        }

        var transaction = _transactionSource.BeginTransaction();
        if (TryParseKeywordExpression(transaction, out var keywordExpression) == ExpressionParsingResult.Success)
        {
            expression = keywordExpression;
            transaction.Commit();
            return ExpressionParsingResult.Success;
        }
        transaction.Rollback();

        return TryParseWithPrecedence(out expression);
    }

    internal ExpressionParsingResult TryParseKeywordExpression(IParsingTransaction transaction, out NodeBase expression)
    {
        expression = null;
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(_ => true, _ => { }, out var keyword))
            return ExpressionParsingResult.FailedNeedPass;
        
        switch (keyword.Keyword)
        {
            case Keywords.Break:
                expression = new BreakNode();
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                return ExpressionParsingResult.Success;
            case Keywords.Continue:
                expression = new ContinueNode();
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                return ExpressionParsingResult.Success;
            case Keywords.Return:
                TryParseWithPrecedence(out var precedence);
                expression = new ReturnNode(precedence);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                return ExpressionParsingResult.Success;
            case Keywords.If:
                var res = TryParseConditionalExpression(transaction, out var node);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                expression = node;
                return res;
            case Keywords.For:
                res = TryParseForLoop(transaction, out var forNode);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                expression = forNode;
                return res;
            case Keywords.While:
                _tokenSequence.RollBackToNonWhiteSpace();
                res = TryParseWhileLoop(transaction, out var whileNode);
                expression = whileNode;
                return res;
            default:
                return ExpressionParsingResult.FailedNeedPass;
        }
    }

    internal ExpressionParsingResult TryParseConditionalExpression(IParsingTransaction transaction, out ConditionNode conditionNode)
    {
        conditionNode = null;
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.If, _ => { }, out _))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }

        TryParseConditionClause(transaction, out var baseClause);
        
        var elifClauses = new List<ClauseNode>();

        var inner = _transactionSource.BeginTransaction();
        while (TryParseEmpty(out _) == ExpressionParsingResult.Success
               || TryParseScopedWithDepth<KeywordToken>(TryParseElifKeyword, out _) == ExpressionParsingResult.Success)
        {
            TryParseConditionClause(inner, out var elifClause);
            elifClauses.Add(elifClause);
        }
        inner.Rollback();
        
        var elseBody = default(BodyNode);
        if (TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.Else, _ => {}, out _))
        {
            elseBody = ParseOptionalBody(transaction);
        }
            
        conditionNode = new ConditionNode(baseClause, elifClauses, elseBody);
        return ExpressionParsingResult.Success;

        ExpressionParsingResult TryParseElifKeyword(out KeywordToken res)
        {
             return TryConsumeNextNonWhiteSpace(x => x.Keyword == Keywords.Elif, _ => { }, out res) 
                 ? ExpressionParsingResult.Success : ExpressionParsingResult.FailedNeedRollback;
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
    
    internal ExpressionParsingResult TryParseWithPrecedence(out NodeBase node, int rbp)
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
        
        if (TryConsumeNextNonWhiteSpace<OperatorToken>(_ => true, _ => { }, out var token))
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
        var typ = default(NodeBase);
        if (TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.Var, _ => { }, out _) 
            || TryParseType(transaction, out typ) == ExpressionParsingResult.Success)
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

    internal ExpressionParsingResult TryParseCastOperator(IParsingTransaction transaction, out NodeBase cast)
    {
        cast = null;
        return TryParseInParen<NodeBase, OpenParen, CloseParen>(
            transaction, TryParseTypeWrapper(transaction),
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
        
        if (TryParseType(transaction, out var type) != ExpressionParsingResult.Success)
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
                OperatorEnum.Decrement => new PrefixDecrementNode(inner),
                _ => throw new ArgumentOutOfRangeException()
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
        if (TryConsumeNextNonWhiteSpace<TClose>(_ => true, _ => { }, out var close))
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

    internal bool TryConsumeNext<TToken>(Func<TToken, bool> predicate, Action<TokenBase> ifPredicateFalse,
        out TToken token) where TToken : TokenBase
    {
        token = default;
        var next = _tokenSequence.PeekNext();
        if (next is TToken target && predicate(target))
        {
            token = target;
            _tokenSequence.GetNextToken();
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
            throw new Exception();
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