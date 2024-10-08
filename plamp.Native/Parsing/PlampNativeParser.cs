using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        if (tokenRes.Exceptions.Any())
        {
            return new ParserResult(null, null, tokenRes.Exceptions);
        }

        _depth = 0;
        _exceptions = [];
        _tokenSequence = tokenRes.Sequence;

        var expressionList = new List<NodeBase>();

        while (_tokenSequence.Current() != null && _tokenSequence.PeekNext() != null)
        {
            if (TryParseTopLevel(out var node))
            {
                expressionList.Add(node);
            }
        }

        return new ParserResult(expressionList, _exceptions, tokenRes.Exceptions);
    }
    
    internal bool TryParseTopLevel(out NodeBase resultNode)
    {
        var currentStart = _tokenSequence.CurrentStart;
        resultNode = null;
        var token = _tokenSequence.PeekNextNonWhiteSpace();
        if (token is Word word)
        {
            switch (word.ToKeyword())
            {
                case Keywords.Def:
                    if (!TryParseFunction(out var defNode))
                    {
                        return false;
                    }

                    resultNode = defNode;
                    return true;

                case Keywords.Use:
                    if (!TryParseUsing(out var useNode))
                    {
                        return false;
                    }

                    resultNode = useNode;
                    return true;

                default:
                    if (TryParseScopedWithDepth<EmptyNode>(TryParseEmpty, out var node, _depth))
                    {
                        resultNode = node;
                        return true;
                    }

                    break;
            }

        }

        AdvanceToRequestedToken<EndOfLine>();
        _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedTopLevel, currentStart, _tokenSequence.CurrentEnd));
        return false;
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
        var currentStart = _tokenSequence.CurrentStart;
        if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Use, () => { }, out _))
        {
            if (TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(
                    x => x.ToKeyword() == Keywords.Unknown, 
                    AddKeywordException, 
                    () => _exceptions.Add(
                        new ParserException(ParserErrorConstants.ExpectedAssemblyName, _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd)), 
                    out var word))
            {
                if (TryConsumeNextNonWhiteSpaceWithoutRollback<EndOfLine>(_ => true,
                        () => { }, () => { }, out _))
                {
                    node = new UseNode(new MemberNode(word.GetString()));
                    return true;
                }
                AdvanceToRequestedToken<EndOfLine>();
                AddParserException(ParserErrorConstants.ExpectedEndOfLine);
                return false;
            }
            AdvanceToRequestedToken<EndOfLine>();
            return false;
        }
        AddParserException(ParserErrorConstants.ExpectedUseStatement);
        return false;
        void AddParserException(string expected) => _exceptions.Add(new ParserException(expected, currentStart, _tokenSequence.CurrentEnd));
    }

    internal bool TryParseFunction(out DefNode node)
    {
        var start = _tokenSequence.CurrentStart;
        node = null;
        if (!TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(x => x.ToKeyword() == Keywords.Def, AddKeywordException, () => {}, out _))
        {
            AddParserException(ParserErrorConstants.ExpectedDefStatement);
            return false;
        }

        var isGetReturnType = TryParseType(out var typeNode);
        var isGetName = TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Unknown, AddKeywordException, out var word);

        var isGetArgs =
            TryParseInParen<List<ParameterNode>, OpenParen, CloseParen>(
                WrapParseCommaSeparated<ParameterNode>(TryParseParameter),
                () => [], out var parameterNodes);
        AdvanceToEndOfLineAndAddException();
        
        var isParsedBody = TryParseBody(out var body);
        if (isGetReturnType && isGetName && isGetArgs && isParsedBody)
        {
            node = new DefNode(typeNode, new MemberNode(word.GetString()), parameterNodes, body);
            return true;
        }

        return false;
        void AddParserException(string expected) => _exceptions.Add(new ParserException(expected, start, _tokenSequence.CurrentEnd));
    }

    internal bool TryParseBody(out BodyNode body)
    {
        body = null;
        using var handle = _depth.EnterNewScope();
        var expressions = new List<NodeBase>();
        while (TryParseScopedWithDepth<NodeBase>(TryParseSingleLineExpression, out var expression))
        {
            expressions.Add(expression);
            AdvanceToEndOfLineAndAddException();
        }

        body = new BodyNode(expressions);
        return true;
    }


    internal bool TryParseParameter(out ParameterNode parameterNode)
    {
        var start = _tokenSequence.CurrentStart;
        parameterNode = null;
        var isTypeParsed = TryParseType(out var type);
        var isWordParsed = TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Unknown,
            AddKeywordException, out var word);
        
        if (isTypeParsed && isWordParsed)
        {
            parameterNode = new ParameterNode(type, new MemberNode(word.GetString()));
            return true;
        }   
        _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedParameter, start, _tokenSequence.CurrentEnd));
        return false;
    }
    
    internal bool TryParseType(out TypeNode typeNode, bool isStrict = true)
    {
        typeNode = null;
        NodeBase last = null;
        while (TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(
                   x => x.ToKeyword() == Keywords.Unknown,
                isStrict ? AddKeywordException : () => { }, 
                isStrict 
                    ? () => _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedWordPartTypeName, _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd)) 
                    : () => {}, out var word))
        {
            if (last == null)
            {
                last = new MemberNode(word.GetString());
            }
            else
            {
                last = new MemberAccessNode(last, new MemberNode(word.GetString()));
            }

            if (!TryConsumeNextNonWhiteSpace<Operator>(x => x.ToOperator() == OperatorEnum.Call, () => { }, out _))
            {
                break;
            }
        }

        if (last == null)
        {
            return false;
        }

        var next = _tokenSequence.PeekNextNonWhiteSpace();
        if (next is not OpenAngleBracket)
        {
            typeNode = new TypeNode(last, []);
            return true;
        }

        var genericStart = _tokenSequence.CurrentStart;
        if (!TryParseInParen<List<TypeNode>, OpenAngleBracket, CloseAngleBracket>(
                WrapParseCommaSeparated<TypeNode>(TryParseTypeWrapper),
                () => [], out var list, isStrict))
        {
            return false;
        }

        if (!list.Any())
        {
            if (isStrict)
            {
                _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedInnerGenerics, genericStart, _tokenSequence.CurrentEnd));
            }
            return false;
        }
        typeNode = new TypeNode(last, list);
        return true;

        bool TryParseTypeWrapper(out TypeNode typeNode)
        {
            return TryParseType(out typeNode, isStrict);
        }
    }

    internal bool TryParseScopedWithDepth<TReturn>(TryParseInternal<TReturn> @internal, out TReturn result,
        int depth = -1)
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

        if (@internal(out result))
        {
            return true;
        }

        _tokenSequence.Position = position;
        return false;
    }

    internal bool TryParseSingleLineExpression(out NodeBase expression)
    {
        expression = null;
        if (TryParseEmpty(out var emptyNode))
        {
            expression = emptyNode;
            return true;
        }
        
        if (_tokenSequence.PeekNextNonWhiteSpace() is Word word
            && word.ToKeyword() != Keywords.Unknown && word.ToKeyword() != Keywords.Var && word.ToKeyword() != Keywords.Await)
        {
            if (TryParseKeywordExpression(out var keywordExpression))
            {
                expression = keywordExpression;
                AdvanceToEndOfLineAndAddException();
                return true;
            }

            TryParseSingleLineExpression(out expression);
            return false;
        }

        return TryParseExpression(out expression) || TryParseSingleLineExpression(out expression);
    }

    internal bool TryParseKeywordExpression(out NodeBase expression)
    {
        expression = null;
        if (TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(x => x.ToKeyword() != Keywords.Unknown,
                () => _exceptions.Add(new ParserException("keyword", _tokenSequence.CurrentStart, _tokenSequence.CurrentStart)),
                AddUnexpectedToken<Word>, out var keyword))
        {
            switch (keyword.ToKeyword())
            {
                case Keywords.Break:
                    expression = new BreakNode();
                    return true;
                case Keywords.Continue:
                    expression = new ContinueNode();
                    return true;
                case Keywords.Return:
                    if (!TryParseExpression(out expression))
                    {
                        return false;
                    }
                    expression = new ReturnNode(expression);
                    return true;
                case Keywords.If:
                    _tokenSequence.RollBackToNonWhiteSpace();
                    if (!TryParseConditionalExpression(out var node))
                    {
                        return false;
                    }

                    expression = node;
                    return true;
                case Keywords.For:
                    _tokenSequence.RollBackToNonWhiteSpace();
                    if (!TryParseForLoop(out var forNode))
                    {
                        return false;
                    }

                    expression = forNode;
                    return true;
                case Keywords.While:
                    _tokenSequence.RollBackToNonWhiteSpace();
                    if (!TryParseWhileLoop(out var whileNode))
                    {
                        return false;
                    }

                    expression = whileNode;
                    return true;
                default:
                    _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedBodyLevelKeyword, _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd));
                    return false;
            }
        }
        return false;
    }

    internal bool TryParseConditionalExpression(out ConditionNode conditionNode)
    {
        var conditionStart = _tokenSequence.CurrentStart;
        conditionNode = null;
        if (!TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(x => x.ToKeyword() == Keywords.If,
                () => _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedIfKeyword, _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd)),
                AddUnexpectedToken<Word>, out _))
        {
            return false;
        }

        var isConditionParsed = TryParseConditionClause(out var baseClause);
        var elifClauses = new List<ClauseNode>();
        Word word;
        while (TryParseEmpty(out _) || TryParseScopedWithDepth(TryParseElifKeyword, out word))
        {
            var clauseStart = _tokenSequence.CurrentStart;
            if (TryParseConditionClause(out var elifClause))
            {
                elifClauses.Add(elifClause);
            }
            else
            {
                _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedElifClause, clauseStart, _tokenSequence.CurrentEnd));   
            }
        }

        var elseBody = default(BodyNode);
        if (word.ToKeyword() == Keywords.Else)
        {
            AdvanceToEndOfLineAndAddException();
            var elseStart = _tokenSequence.CurrentStart;
            if (!TryParseBody(out elseBody))
            {
                _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedElseClause, elseStart, _tokenSequence.CurrentEnd));
            }
        }
        else
        {
            RollBackToRequestedNonWhiteSpaceToken<EndOfLine>();
        }

        if (isConditionParsed)
        {
            conditionNode = new ConditionNode(baseClause, elifClauses, elseBody);
            return true;
        }
        _exceptions.Add(new ParserException(ParserErrorConstants.InvalidConditionBlock, conditionStart, _tokenSequence.CurrentEnd));
        return false;

        bool TryParseElifKeyword(out Word res)
        {
             return TryConsumeNextNonWhiteSpace(x => x.ToKeyword() == Keywords.Elif, () => { }, out res);
        }
    }

    internal bool TryParseConditionClause(out ClauseNode conditionNode)
    {
        var isPredicateParsed = TryParseInParen<NodeBase, OpenParen, CloseParen>(TryParseExpression,
            () =>
            {
                _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedConditionExpression, _tokenSequence.CurrentStart,
                    _tokenSequence.CurrentEnd));
                return null;
            }, out var expression);
        AdvanceToEndOfLineAndAddException();
        var isParsedClause = TryParseBody(out var body);
        if (isPredicateParsed && isParsedClause)
        {
            conditionNode = new ClauseNode(expression, body);
            return true;
        }

        conditionNode = null;
        return false;
    }

    internal bool TryParseForLoop(out ForNode forNode)
    {
        forNode = null;
        if (!TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(x => x.ToKeyword() == Keywords.For,
                () => _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedForKeyword, _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd)),
                AddUnexpectedToken<Word>, out _))
        {
            return false;
        }

        var isHeaderParsed = TryParseInParen<ForHeaderHolder, OpenParen, CloseParen>(TryParseForHeader, () =>
        {
            _exceptions.Add(new ParserException(ParserErrorConstants.InvalidForHeaderDefinition, _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd));
            return default;
        }, out var result);
        AdvanceToEndOfLineAndAddException();
        var isParseBody = TryParseBody(out var body);
        if (isHeaderParsed && isParseBody)
        {
            forNode = new ForNode(result.IteratorVar, result.Iterable, body);
            return true;
        }

        return false;
    }

    internal record struct ForHeaderHolder(VariableDefinitionNode IteratorVar, NodeBase Iterable);

    internal bool TryParseForHeader(out ForHeaderHolder headerHolder)
    {
        var isDefinitionParsed = TryParseCreateVariable(out var node);
        var isKeywordParsed = TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.In,
            () => _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedInKeyword, _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd)),
            out _);
        var isExpressionParsed = TryParseExpression(out var expression);
        if (isDefinitionParsed && isExpressionParsed && isKeywordParsed)
        {
            headerHolder = new ForHeaderHolder(node, expression);
            return true;
        }

        headerHolder = default;
        return false;
    }
    
    internal bool TryParseWhileLoop(out WhileNode whileNode)
    {
        whileNode = null;
        if (!TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(x => x.ToKeyword() == Keywords.While,
                () => _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedWhileKeyword, _tokenSequence.CurrentStart, _tokenSequence.CurrentEnd)),
                AddUnexpectedToken<Word>, out _))
        {
            return false;
        }

        var isParsedPredicate = TryParseInParen<NodeBase, OpenParen, CloseParen>(TryParseExpression, () =>
        {
            _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedConditionExpression, _tokenSequence.CurrentStart,
                _tokenSequence.CurrentEnd));
            return null;
        }, out var expression);
        AdvanceToEndOfLineAndAddException();
        
        var isBodyParsed = TryParseBody(out var body);
        if (isBodyParsed && isParsedPredicate)
        {
            whileNode = new WhileNode(expression, body);
            return true;
        }

        return false;
    }

    internal bool TryParseCreateVariable(out VariableDefinitionNode variableDefinitionNode)
    {
        var start = _tokenSequence.CurrentStart;
        variableDefinitionNode = null;
        var isTypeParsed = TryParseType(out var type);
        var isWordParsed = TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Unknown,
            AddKeywordException, out var word);
        
        if (isTypeParsed && isWordParsed)
        {
            variableDefinitionNode = new VariableDefinitionNode(type, new MemberNode(word.GetString()));
            return true;
        }   
        _exceptions.Add(new ParserException(ParserErrorConstants.ExpectedVariableDefinition, start, _tokenSequence.CurrentEnd));
        return false;
    }

    internal bool TryParseExpression(out NodeBase expression)
    {
        
        if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Var, () => { }, out _))
        {
            if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Unknown, () => { }, out var name))
            {
                var definition = new VariableDefinitionNode(null, new MemberNode(name.GetString()));
                return TryParseWithPrecedence(out expression, 0, definition);
            }

            expression = null;
            return false;
        }
        
        var start = _tokenSequence.Position;
        if (!TryParseType(out var type, false))
        {
            _tokenSequence.Position = start;
            return TryParseWithPrecedence(out expression);
        }
        
        if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Unknown, () => { }, out var varName))
        {
            var definition = new VariableDefinitionNode(type, new MemberNode(varName.GetString()));
            return TryParseWithPrecedence(out expression, 0, definition);
        }
        _tokenSequence.Position = start;

        return TryParseWithPrecedence(out expression);
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

    internal bool TryParseNud(out NodeBase node, bool isParseCast = true)
    {
        if (isParseCast)
        {
            var position = _tokenSequence.Position;
            var next = _tokenSequence.PeekNextNonWhiteSpace();
            if (next != null && next.GetType() == typeof(OpenParen) 
                             && TryParseInParen<TypeNode, OpenParen, CloseParen>(TryParseTypeWrapper, () => null, out var type) 
                             && TryParseNud(out var inner, false))
            {
                node = new CastNode(type, inner);
            }

            _tokenSequence.Position = position;
        }

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
                TryParsePrecedenceWrapper, () => { 
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
            var stringLiteral = new ConstNode(literal.GetString());
            node = ParsePostfixIfExist(stringLiteral);
            return true;
        }
        
        if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.New, () => { }, out _))
        {
            if (TryParseType(out var type)
                && TryParseInParen<List<NodeBase>, OpenParen, CloseParen>(
                    WrapParseCommaSeparated<NodeBase>(TryParseExpression),
                    () => [], out var args))
            {
                var ctor = new ConstructorNode(type, args);
                node = ParsePostfixIfExist(ctor);
                return true;
            }
            node = null;
            return false;
        }

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

        if (TryConsumeNextNonWhiteSpace<Word>(w => w.ToKeyword() == Keywords.Unknown, () => { },
                out var word))
        {
            var member = new MemberNode(word.GetString());
            node = ParsePostfixIfExist(member);
            return true;
        }

        node = null;
        return false;
        
        bool TryParsePrecedenceWrapper(out NodeBase node)
        {
            return TryParseWithPrecedence(out node);
        }

        bool TryParseTypeWrapper(out TypeNode typeNode)
        {
            return TryParseType(out typeNode, false);
        }
    }
    
    internal NodeBase ParsePostfixIfExist(NodeBase inner)
    {
        while (TryParseCall(inner, out inner))
        {
            while (_tokenSequence.PeekNextNonWhiteSpace()?.GetType() == typeof(OpenSquareBracket) 
                   && TryParseIndexer(inner, out inner))
            { }
            
            if (TryConsumeNextNonWhiteSpace<Operator>(
                    x => x.ToOperator() == OperatorEnum.Increment || x.ToOperator() == OperatorEnum.Decrement,
                    () => { }, out var @operator))
            {
                return @operator.ToOperator() switch
                {
                    OperatorEnum.Increment => new PostfixIncrementNode(inner),
                    OperatorEnum.Decrement => new PostfixDecrementNode(inner),
                    _ => throw new Exception("Parser exception")
                };
            }
        }
        
        return inner;
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
                        res = new CallNode(input, new MemberNode(name.GetString()), args);
                        return true;
                    }
                    
                    res = new CallNode(input, new MemberNode(name.GetString()), args);
                    return false;
                }

                _tokenSequence.Position = position;
                res = new MemberAccessNode(input, new MemberNode(name.GetString()));
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
        
        var startPos = new TokenPosition(next.StartPosition);
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
            if (!res || right != null)
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
                        //TODO: отдельная ошибка
                        AddUnexpectedToken<Operator>();
                        _tokenSequence.Position = start;
                        output = left;
                        return false;
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
                AddUnexpectedToken<TOpen>, out _))
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
        if (_tokenSequence.Current() != null && _tokenSequence.Current().GetType() == typeof(EndOfLine))
        {
            return;
        }

        AddExceptionWithShift<object>(ParserErrorConstants.ExpectedEndOfLine, AdvanceWrapper, out _);
        
        bool AdvanceWrapper(out object obj)
        {
            obj = null;
            AdvanceToRequestedToken<EndOfLine>();
            return true;
        }
    }

    internal bool WrapParseExpression(out NodeBase expression)
    {
        var currentStart = _tokenSequence.CurrentStart;
        var start = new TokenPosition(currentStart.Pos + 1);
        var res = TryParseExpression(out expression);
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
        var unexpectedStart = _tokenSequence.CurrentStart.Pos < 0 
            ? _tokenSequence.CurrentStart : new TokenPosition(_tokenSequence.CurrentEnd.Pos + 1);
        var result = @internal(out res);
        var endPos = new TokenPosition(_tokenSequence.CurrentEnd.Pos < 0
            ? _tokenSequence.Position > 0 ? _tokenSequence.Last().EndPosition : -1
            : _tokenSequence.CurrentEnd.Pos);
        _exceptions.Add(new ParserException(errorText, unexpectedStart, endPos));
        return result;
    }
    
    internal void AddNextTokenException(Action exceptAction)
    {
        var pos = _tokenSequence.Position;
        _tokenSequence.GetNextNonWhiteSpace();
        exceptAction();
        _tokenSequence.Position = pos;
    }
    
    #endregion
}