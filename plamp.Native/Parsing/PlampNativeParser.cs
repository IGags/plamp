using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.ControlFlow;
using plamp.Ast.Node.Unary;
using plamp.Native.Enumerations;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Parsing;

public sealed class PlampNativeParser
{
    internal delegate bool TryParseInternal<T>(out T result);
    
    private TokenSequence _tokenSequence;
    private DepthCounter _depth;
    private List<ParserException> _exceptions;
    
    [Obsolete("For test purposes only")]
    internal IReadOnlyList<ParserException> Exceptions => _exceptions;
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
        _exceptions = [];
        _tokenSequence = tokenRes.Sequence;
    }

    public PlampNativeParser(){}
    
    public ParserResult Parse(string code)
    {
        var tokenRes = code.Tokenize();

        _depth = 0;
        _exceptions = [];
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

        return new ParserResult(expressionList, _exceptions, tokenRes.Exceptions);
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
    
    internal bool TryParseType(out TypeNode typeNode, bool isAddException = false)
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

    internal bool TryParseConditionalExpression(out ConditionNode conditionNode)
    {
        conditionNode = null;
        if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.If, () => { }, out _))
        {
            TryParseConditionClause(out var baseClause);
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
        }

        return false;
        bool TryParseElifKeyword(out Word res)
        {
             return TryConsumeNextNonWhiteSpace(x => x.ToKeyword() == Keywords.Elif, () => { }, out res);
        }
    }

    internal bool TryParseConditionClause(out ClauseNode conditionNode)
    {
        var nextStart = new TokenPosition(_tokenSequence.CurrentEnd.Pos + 1);
        if (!TryParseInParen<NodeBase, OpenParen, CloseParen>(PrecedenceWrapper,
                () =>
                {
                    _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedConditionExpression, nextStart,
                        _tokenSequence.CurrentEnd));
                    return null;
                }, out var expression))
        {
            AdvanceToFirstOfTokens([typeof(EndOfLine)]);
        }
        AdvanceToEndOfLineAndAddException();
        TryParseBody(out var body);
        conditionNode = new ClauseNode(expression, body);
        return true;
    }

    internal bool TryParseForLoop(out ForNode forNode)
    {
        forNode = null;
        if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.For, () => { }, out _))
        {
            var nextStart = new TokenPosition(_tokenSequence.CurrentEnd.Pos + 1);
            if (!TryParseInParen<ForHeaderHolder, OpenParen, CloseParen>(TryParseForHeader,
                    () =>
                    {
                        _exceptions.Add(new ParserException(ParserErrorConstants.InvalidForHeaderDefinition, nextStart,
                            _tokenSequence.CurrentEnd));
                        return default;
                    }, out var header))
            {
                AdvanceToFirstOfTokens([typeof(EndOfLine)]);
            }
            AdvanceToEndOfLineAndAddException();
            TryParseBody(out var body);
            forNode = new ForNode(header.IteratorVar, header.Iterable, body);
            return true;
        }
        
        return false;
    }

    internal record struct ForHeaderHolder(NodeBase IteratorVar, NodeBase Iterable);

    internal bool TryParseForHeader(out ForHeaderHolder headerHolder)
    {
        AddExceptionWithShift<NodeBase>(ParserErrorConstants.InvalidExpression, PrecedenceWrapper, out var iteratorVar);
        TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.In,
            () => AddNextTokenException(
                () => _exceptions.Add(new ParserException(
                    ParserErrorConstants.ExpectedInKeyword, _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd))),
            out _);
        AddExceptionWithShift<NodeBase>(ParserErrorConstants.InvalidExpression, PrecedenceWrapper, out var iterable);
        
        headerHolder = new ForHeaderHolder(iteratorVar, iterable);
        return true;
    }
    
    internal bool TryParseWhileLoop(out WhileNode whileNode)
    {
        whileNode = null;
        
        if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.While, () => { }, out _))
        {
            var nextErrorStart = _tokenSequence.CurrentEnd.Pos + 1;
            if (!TryParseInParen<NodeBase, OpenParen, CloseParen>(PrecedenceWrapper, () =>
                {
                    _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedConditionExpression,
                        new TokenPosition(nextErrorStart),
                        _tokenSequence.CurrentEnd));
                    return null;
                }, out var expression))
            {
                AdvanceToFirstOfTokens([typeof(EndOfLine)]);
            }
            
            AdvanceToEndOfLineAndAddException();
            TryParseBody(out var body);
            whileNode = new WhileNode(expression, body);
            return true;
        }
        
        return false;
    }

    internal bool TryParseWithPrecedence(out NodeBase node, int rbp = 0, NodeBase nud = null)
    {
        if (nud == null)
        {
            var isParsedNud = TryParseNud(out node);
            if (!isParsedNud)
            {
                return false;
            }
        }
        else
        {
            node = nud;
        }

        while (TryParseLed(rbp, node, out node))
        {
        }

        return true;
    }

    internal bool TryParseNud(out NodeBase node)
    {
        var position = _tokenSequence.Position;

        var typ = default(TypeNode);
        if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Var, () => { }, out _) 
            || TryParseType(out typ))
        {
            if (_tokenSequence.PeekNextNonWhiteSpace()?.GetType() == typeof(Word) 
                && TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Unknown, () => AddNextTokenException(AddKeywordException), out var name))
            {
                var definition = new VariableDefinitionNode(typ, new MemberNode(name.GetStringRepresentation()));
                return TryParseWithPrecedence(out node, 0, definition);
            }

            _tokenSequence.Position = position;
            if (typ == default)
            {
                node = null;
                return false;
            }
        }
        _tokenSequence.Position = position;
        
        var next = _tokenSequence.PeekNextNonWhiteSpace();
        
        if (next == null || next.GetType() == typeof(EndOfLine))
        {
            node = null;
            return false;
        }
        
        if (next.GetType() == typeof(OpenParen) 
            && TryParseInParen<TypeNode, OpenParen, CloseParen>(TryParseTypeWrapper, () => null, out var typeNode, false))
        {
            if (TryParseNud(out var inCast))
            {
                node = new CastNode(typeNode, inCast);
                return true;
            }
            if (inCast == null)
            {
                _tokenSequence.Position = position;
                node = null;
                return false;
            }
        }
        
        _tokenSequence.Position = position;

        var nextToken = _tokenSequence.PeekNextNonWhiteSpace();
        if (nextToken is null)
        {
            node = null;
            return false;
        }
        
        if (nextToken is OpenParen)
        {
            var start = _tokenSequence.CurrentStart;
            var isParsed = TryParseInParen<NodeBase, OpenParen, CloseParen>(
                PrecedenceWrapper, () => { 
                    _exceptions.Add(new ParserException(ParserErrorConstants.InvalidExpression, start, _tokenSequence.CurrentEnd));
                    return null;
                }, out var inner);
            node = inner;
            
            if (isParsed)
            {
                node = ParsePostfixIfExist(node);
                return true;
            }

            return false;
        }

        if (TryConsumeNextNonWhiteSpace<StringLiteral>(_ => true, () => { }, out var literal))
        {
            var stringLiteral = new ConstNode(literal.GetStringRepresentation());
            node = ParsePostfixIfExist(stringLiteral);
            return true;
        }
        var pos = _tokenSequence.Position;
        if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.New, () => { }, out _))
        {
            
            if (TryParseType(out var type)
                && _tokenSequence.PeekNext()?.GetType() == typeof(OpenParen)
                && TryParseInParen<List<NodeBase>, OpenParen, CloseParen>(
                    WrapParseCommaSeparated<NodeBase>(PrecedenceWrapper),
                    () => [], out var args))
            {
                var ctor = new ConstructorNode(type, args);
                node = ParsePostfixIfExist(ctor);
                return true;
            }
            _tokenSequence.Position = pos;
            node = null;
            return false;
        }
        _tokenSequence.Position = pos;
        
        var backupPos = _tokenSequence.Position;
        if (TryConsumeNextNonWhiteSpace<Operator>(
                x =>
                    x.ToOperator() == OperatorEnum.Minus
                    || x.ToOperator() == OperatorEnum.Not
                    || x.ToOperator() == OperatorEnum.Increment
                    || x.ToOperator() == OperatorEnum.Decrement,
                () => { }, out var operatorToken))
        {
            var op = operatorToken.ToOperator();
            TryParseWithPrecedence(out var inner, op.GetPrecedence(true));
            if (inner != null)
            {
                node = null;
                switch (op)
                {
                    case OperatorEnum.Minus:
                        node = new UnaryMinusNode(inner);
                        break;
                    case OperatorEnum.Not:
                        node = new NotNode(inner);
                        break;
                    case OperatorEnum.Increment:
                        node = new PrefixIncrementNode(inner);
                        break;
                    case OperatorEnum.Decrement:
                        node = new PrefixDecrementNode(inner);
                        break;
                }

                node = ParsePostfixIfExist(node);
                return true;
            }   
        }

        _tokenSequence.Position = backupPos;
        if (TryConsumeNextNonWhiteSpace<Word>(w => w.ToKeyword() == Keywords.Unknown, () => { },
                out var word))
        {
            var member = new MemberNode(word.GetStringRepresentation());
            node = ParsePostfixIfExist(member);
            return true;
        }

        if (TryConsumeNextNonWhiteSpace<Word>(w => w.ToKeyword() == Keywords.True, () => { }, out _))
        {
            node = new ConstNode(true);
            node = ParsePostfixIfExist(node);
            return true;
        }
        
        if (TryConsumeNextNonWhiteSpace<Word>(w => w.ToKeyword() == Keywords.False, () => { }, out _))
        {
            node = new ConstNode(false);
            node = ParsePostfixIfExist(node);
            return true;
        }

        node = null;
        return false;

        bool TryParseTypeWrapper(out TypeNode type)
        {
            return TryParseType(out type);
        }
    }
    
    internal NodeBase ParsePostfixIfExist(NodeBase inner)
    {
        do
        {
            inner = TryParsePostfixOperator(inner);
            
            while (_tokenSequence.PeekNextNonWhiteSpace()?.GetType() == typeof(OpenSquareBracket)
                   && TryParseIndexer(inner, out inner))
            {
            }

            inner = TryParsePostfixOperator(inner);
        } while (TryParseCall(inner, out inner));
        
        
        return inner;
    }

    internal NodeBase TryParsePostfixOperator(NodeBase nodeBase)
    {
        if (TryConsumeNextNonWhiteSpace<Operator>(
                x => x.ToOperator() == OperatorEnum.Increment || x.ToOperator() == OperatorEnum.Decrement,
                () => { }, out var @operator))
        {
            return @operator.ToOperator() switch
            {
                OperatorEnum.Increment => new PostfixIncrementNode(nodeBase),
                OperatorEnum.Decrement => new PostfixDecrementNode(nodeBase),
                _ => throw new Exception("Parser exception")
            };
        }

        return nodeBase;
    }
    
    internal bool TryParseCall(NodeBase input, out NodeBase res)
    {
        if (TryConsumeNextNonWhiteSpace<Operator>(x => x.ToOperator() == OperatorEnum.Call, () => { }, out _))
        {
            var nextWord = _tokenSequence.PeekNextNonWhiteSpace();
            if (nextWord == null || nextWord.GetType() != typeof(Word))
            {
                AddNextTokenException(AddUnexpectedToken<Word>);
                res = input;
                return false;
            }
            
            if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Unknown, 
                    () => AddNextTokenException(AddKeywordException), out var name))
            {
                var next = _tokenSequence.PeekNextNonWhiteSpace();
                var position = _tokenSequence.Position;
                if (next is not null && next.GetType() == typeof(OpenParen))
                {
                    if (TryParseInParen<List<NodeBase>, OpenParen, CloseParen>(
                            WrapParseCommaSeparated<NodeBase>(WrapParseExpression),
                            () => [], out var args))
                    {
                        res = new CallNode(input, new MemberNode(name.GetStringRepresentation()), args);
                        return true;
                    }
                    
                    res = new CallNode(input, new MemberNode(name.GetStringRepresentation()), args);
                    return false;
                }

                _tokenSequence.Position = position;
                res = new MemberAccessNode(input, new MemberNode(name.GetStringRepresentation()));
                return true;
            }
        }

        res = input;
        return false;
    }
    
    internal bool TryParseIndexer(NodeBase inner, out NodeBase node)
    {
        var next = _tokenSequence.PeekNextNonWhiteSpace();
        if (next is null || next.GetType() != typeof(OpenSquareBracket))
        {
            node = inner;
            return false;
        }
        
        var startPos = new TokenPosition(next.Start);
        var isParsed = TryParseInParen<List<NodeBase>, OpenSquareBracket, CloseSquareBracket>(
            WrapParseCommaSeparated<NodeBase>(WrapParseExpression), () =>
            {
                _exceptions.Add(new ParserException(ParserErrorConstants.EmptyIndexerDefinition, startPos, _tokenSequence.CurrentEnd));
                return [];
            }, out var index);
        if (isParsed)
        {
            node = new IndexerNode(inner, index);
            return true;
        }
        

        node = inner;
        return false;
    }

    internal bool TryParseLed(int rbp, NodeBase left, out NodeBase output)
    {
        var start = _tokenSequence.Position;
        if (TryConsumeNextNonWhiteSpace<Operator>(_ => true, () => { }, out var token))
        {
            var op = token.ToOperator();
            var precedence = op.GetPrecedence(false);
            if (precedence <= rbp)
            {
                output = left;
                _tokenSequence.RollBackToNonWhiteSpace();
                return false;
            }

            var res = TryParseWithPrecedence(out var right, precedence);
            if (res && right != null)
            {
                switch (op)
                {
                    case OperatorEnum.Multiply:
                        output = new MultiplyNode(left, right);
                        return true;
                    case OperatorEnum.Divide:
                        output = new DivideNode(left, right);
                        return true;
                    case OperatorEnum.Plus:
                        output = new PlusNode(left, right);
                        return true;
                    case OperatorEnum.Minus:
                        output = new MinusNode(left, right);
                        return true;
                    case OperatorEnum.Lesser:
                        output = new LessNode(left, right);
                        return true;
                    case OperatorEnum.Greater:
                        output = new GreaterNode(left, right);
                        return true;
                    case OperatorEnum.LesserOrEquals:
                        output = new LessOrEqualNode(left, right);
                        return true;
                    case OperatorEnum.GreaterOrEquals:
                        output = new GreaterOrEqualsNode(left, right);
                        return true;
                    case OperatorEnum.Equals:
                        output = new EqualNode(left, right);
                        return true;
                    case OperatorEnum.NotEquals:
                        output = new NotEqualNode(left, right);
                        return true;
                    case OperatorEnum.And:
                        output = new AndNode(left, right);
                        return true;
                    case OperatorEnum.Or:
                        output = new OrNode(left, right);
                        return true;
                    case OperatorEnum.Modulo:
                        output = new ModuloNode(left, right);
                        return true;
                    case OperatorEnum.Assign:
                        output = new AssignNode(left, right);
                        return true;
                    case OperatorEnum.PlusAndAssign:
                        output = new AddAndAssignNode(left, right);
                        return true;
                    case OperatorEnum.MinusAndAssign:
                        output = new SubAndAssignNode(left, right);
                        return true;
                    case OperatorEnum.MultiplyAndAssign:
                        output = new MulAndAssignNode(left, right);
                        return true;
                    case OperatorEnum.DivideAndAssign:
                        output = new DivAndAssignNode(left, right);
                        return true;
                    case OperatorEnum.ModuloAndAssign:
                        output = new ModuloAndAssignNode(left, right);
                        return true;
                    case OperatorEnum.AndAndAssign:
                        output = new AndAndAssignNode(left, right);
                        return true;
                    case OperatorEnum.OrAndAssign:
                        output = new OrAndAssignNode(left, right);
                        return true;
                    case OperatorEnum.XorAndAssign:
                        output = new XorAndAssignNode(left, right);
                        return true;
                    case OperatorEnum.BitwiseAnd:
                        output = new BitwiseAndNode(left, right);
                        return true;
                    case OperatorEnum.BitwiseOr:
                        output = new BitwiseOrNode(left, right);
                        return true;
                    case OperatorEnum.Xor:
                        output = new XorNode(left, right);
                        return true;
                    default:
                        throw new Exception();
                }
            }
            
            output = left;
            _tokenSequence.Position = start;
            return false;
        }

        output = left;
        return false;
    }

    #region Helper
    
    internal bool TryParseInParen<TResult, TOpen, TClose>(TryParseInternal<TResult> parserFunc, Func<TResult> emptyCase, out TResult result, bool isStrict = true)
        where TOpen : TokenBase where TClose : TokenBase
    {
        result = default;
        var next = _tokenSequence.PeekNext();
        if (!isStrict && (next == null || next.GetType() != typeof(TOpen)))
        {
            return false;
        }
        
        if (!TryConsumeNextNonWhiteSpace<TOpen>(_ => true,
                () => AddNextTokenException(AddUnexpectedToken<TOpen>), out _))
        {
            return false;
        }
        if (TryConsumeNextNonWhiteSpace<TClose>(_ => true, () => { }, out _))
        {
            result = emptyCase();
            return true;
        }

        var res = parserFunc(out result);
        if (TryConsumeNextNonWhiteSpace<TClose>(_ => true, () => { }, out _))
        {
            return res;
        }

        if (!isStrict)
        {
            return false;
        }

        var unexpectedStart = _tokenSequence.Position >= _tokenSequence.TokenList.Count 
            ? _tokenSequence.CurrentStart : new TokenPosition(_tokenSequence.CurrentStart.Pos + 1);
        
        AdvanceToFirstOfTokens([typeof(EndOfLine), typeof(TClose)]);
        var endPos = _tokenSequence.CurrentEnd;
        if (_tokenSequence.Current() is TClose)
        {
            endPos = new TokenPosition(endPos.Pos - 1);
        }
        //TODO: своя ошибка под каждую скобку
        _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedCloseParen, unexpectedStart, endPos));
        return false;
    }

    internal bool TryParseCommaSeparated<TReturn>(TryParseInternal<TReturn> parserFunc, out List<TReturn> result)
    {
        result = [];
        var accumulate = true;
        while (true)
        {
            accumulate &= parserFunc(out var res);
            result.Add(res);
            
            if (!TryConsumeNextNonWhiteSpace<Comma>(_ => true, () => {}, out _))
            {
                return accumulate;
            }
        }
    }

    internal bool TryConsumeNextNonWhiteSpace<TToken>(Func<TToken, bool> predicate, Action ifPredicateFalse, out TToken token)
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

        ifPredicateFalse();
        return false;
    }

    internal bool TryConsumeNextNonWhiteSpaceWithoutRollback<TToken>(Func<TToken, bool> predicate, 
        Action ifPredicateFalse, Action ifTokenMismatch, out TToken token)
        where TToken : TokenBase
    {
        token = null;
        if (typeof(TToken) == typeof(WhiteSpace))
        {
            return false;
        }
        var next = _tokenSequence.GetNextNonWhiteSpace();
        if (next is TToken target)
        {
            if (predicate(target))
            {
                token = target;
                return true;
            }

            ifPredicateFalse();
            return false;
        }

        ifTokenMismatch();
        return false;
    }

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

    private TryParseInternal<List<TReturn>> WrapParseCommaSeparated<TReturn>(TryParseInternal<TReturn> parserFunc)
    {
        return FuncWrapper;
        bool FuncWrapper(out List<TReturn> resultList)
        {
            return TryParseCommaSeparated(parserFunc, out resultList);
        }
    }
    
    internal void AddUnexpectedToken<T>() where T : TokenBase =>
        _exceptions.Add(new ParserException($"{ParserErrorConstants.UnexpectedTokenPrefix} {typeof(T).Name}", _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd));

    internal void AdvanceToEndOfLineAndAddException()
    {
        if (_tokenSequence.PeekNextNonWhiteSpace() == null 
            || _tokenSequence.PeekNextNonWhiteSpace()?.GetType() == typeof(EndOfLine))
        {
            _tokenSequence.GetNextToken();
            return;
        }
        
        if ((_tokenSequence.Current() == null && _tokenSequence.PeekNext() == null) 
            || _tokenSequence.Current()?.GetType() == typeof(EndOfLine))
        {
            return;
        }

        AddExceptionWithShift<object>(ParserErrorConstants.ExpectedEndOfLine, AdvanceWrapper, out _);
        
        bool AdvanceWrapper(out object obj)
        {
            obj = null;
            AdvanceToRequestedToken<EndOfLine>();
            return false;
        }
    }

    internal bool WrapParseExpression(out NodeBase expression)
    {
        var currentStart = _tokenSequence.CurrentStart;
        var start = new TokenPosition(currentStart.Pos + 1);
        var res = TryParseWithPrecedence(out expression);
        if (!res)
        {
            var end = _tokenSequence.CurrentEnd;
            if (currentStart.Pos == end.Pos)
            {
                end = new TokenPosition(end.Pos + 1);
            }
                
            _exceptions.Add(new ParserException(ParserErrorConstants.InvalidExpression, start, end));
        }
        return true;
    }
    
    internal bool AddExceptionWithShift<T>(string errorText, TryParseInternal<T> @internal, out T res)
    {
        var start = _tokenSequence.Position;
        var unexpectedStart = _tokenSequence.CurrentStart.Pos < 0 
            ? _tokenSequence.CurrentStart : new TokenPosition(_tokenSequence.CurrentEnd.Pos + 1);
        var result = @internal(out res);
        if (result) 
        {
            return true;
        }
        TokenPosition endPos;
        if (_tokenSequence.Any() && _tokenSequence.Current() == null && _tokenSequence.Position > -1)
        {
            endPos = new TokenPosition(_tokenSequence.Last().End + 1);
        }
        else
        //TODO: мерзкий кусок кода
            endPos = _tokenSequence.Position switch
            {
                > -1 when !_tokenSequence.Any() => new TokenPosition(0),
                < 0 => new TokenPosition(-1),
                _ when start != _tokenSequence.Position => _tokenSequence.CurrentEnd,
                _ => new TokenPosition(_tokenSequence.CurrentEnd.Pos + 1)
            };

        _exceptions.Add(new ParserException(errorText, unexpectedStart, endPos));
        return false;
    }
    
    internal void AddNextTokenException(Action exceptAction)
    {
        var pos = _tokenSequence.Position;
        _tokenSequence.GetNextNonWhiteSpace();
        exceptAction();
        _tokenSequence.Position = pos;
    }
    
    private bool PrecedenceWrapper(out NodeBase inner)
    {
        return TryParseWithPrecedence(out inner);
    }

    private string ParseDotSeparatedName(string exception, bool isAddException)
    {
        StringBuilder last = null;
        var pos = _tokenSequence.Position;
        var definitionStart = new TokenPosition(_tokenSequence.CurrentEnd.Pos + 1);
        while (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Unknown, () =>
               {
                   if (isAddException)
                   {
                       var end = _tokenSequence.Position == pos
                           ? new TokenPosition(_tokenSequence.PeekNextNonWhiteSpace()?.End ?? definitionStart.Pos)
                           : _tokenSequence.CurrentEnd;
                       _exceptions.Add(new ParserException(exception, definitionStart, end));
                   }
               }, out var word))
        {
            if (last == null)
            {
                last = new StringBuilder();
                last.Append(word.GetStringRepresentation());
            }
            else
            {
                last.Append($".{word.GetStringRepresentation()}");
            }
            
            if (!TryConsumeNextNonWhiteSpace<Operator>(x => x.ToOperator() == OperatorEnum.Call, () => { }, out _))
            {
                break;
            }
        }

        return last?.ToString();
    }
    
    #endregion
}