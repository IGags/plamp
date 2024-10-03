using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.ControlFlow;
using plamp.Native.Enumerations;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Parsing;

public class PlampNativeParser
{
    private delegate bool TryParseInternal<T>(out T result);

    private TokenSequence _tokenSequence;
    private DepthCounter _depth;
    private List<ParserException> _exceptions;

    public PlampNativeParser()
    {
    }

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

    private bool TryParseTopLevel(out NodeBase resultNode)
    {
        var startPosition = _tokenSequence.Position;
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
        _exceptions.Add(new ParserException("use or def statement", startPosition, _tokenSequence.Position));
        return false;
    }

    private bool TryParseEmpty(out EmptyNode node)
    {
        if (TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, () => {}, out _))
        {
            node = new EmptyNode();
            return true;
        }

        node = null;
        return false;
    }

    private bool TryParseUsing(out UseNode node)
    {
        node = null;
        var start = _tokenSequence.Position;
        if (TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Use, () => { }, out _))
        {
            if (TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(
                    x => x.ToKeyword() == Keywords.Unknown, 
                    AddKeywordException, 
                    () => _exceptions.Add(
                        new ParserException("a valid assembly name", _tokenSequence.Position, _tokenSequence.Position)), 
                    out var word))
            {
                start = _tokenSequence.Position;
                if (TryConsumeNextNonWhiteSpaceWithoutRollback<EndOfLine>(_ => true,
                        () => { }, () => { }, out _))
                {
                    node = new UseNode(new MemberNode(word.GetString()));
                    return true;
                }
                AdvanceToRequestedToken<EndOfLine>();
                AddParserException("end of line");
                return false;
            }
            AdvanceToRequestedToken<EndOfLine>();
            return false;
        }
        AddParserException("use statement");
        return false;
        void AddParserException(string expected) => _exceptions.Add(new ParserException(expected, start, _tokenSequence.Position));
    }

    private bool TryParseFunction(out DefNode node)
    {
        var start = _tokenSequence.Position;
        node = null;
        if (!TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(x => x.ToKeyword() == Keywords.Def, AddKeywordException, () => {}, out _))
        {
            AddParserException("def statement");
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
        void AddParserException(string expected) => _exceptions.Add(new ParserException(expected, start, _tokenSequence.Position));
    }

    private bool TryParseBody(out BodyNode body)
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


    private bool TryParseParameter(out ParameterNode parameterNode)
    {
        var start = _tokenSequence.Position;
        parameterNode = null;
        var isTypeParsed = TryParseType(out var type);
        var isWordParsed = TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Unknown,
            AddKeywordException, out var word);
        
        if (isTypeParsed && isWordParsed)
        {
            parameterNode = new ParameterNode(type, new MemberNode(word.GetString()));
            return true;
        }   
        _exceptions.Add(new ParserException("parameter defenition", start, _tokenSequence.Position));
        return false;
    }
    
    private bool TryParseType(out TypeNode typeNode)
    {
        typeNode = null;
        if (!TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(x => x.ToKeyword() == Keywords.Unknown,
                AddKeywordException, 
                () => _exceptions.Add(new ParserException("type name", _tokenSequence.Position, _tokenSequence.Position)),
                out var word))
        {
            return false;
        }

        var next = _tokenSequence.PeekNextNonWhiteSpace();
        if (next is not OpenAngleBracket)
        {
            typeNode = new TypeNode(word.GetString(), []);
            return true;
        }

        var genericStart = _tokenSequence.Position;
        if (!TryParseInParen<List<TypeNode>, OpenAngleBracket, CloseAngleBracket>(
                WrapParseCommaSeparated<TypeNode>(TryParseType),
                () => [], out var list))
        {
            return false;
        }

        if (!list.Any())
        {
            _exceptions.Add(new ParserException("generic definition", genericStart, _tokenSequence.Position));
            return false;
        }
        typeNode = new TypeNode(word.GetString(), list);
        return true;
    }

    private bool TryParseScopedWithDepth<TReturn>(TryParseInternal<TReturn> @internal, out TReturn result,
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

    private bool TryParseSingleLineExpression(out NodeBase expression)
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

    private bool TryParseKeywordExpression(out NodeBase expression)
    {
        expression = null;
        if (TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(x => x.ToKeyword() != Keywords.Unknown,
                () => _exceptions.Add(new ParserException("keyword", _tokenSequence.Position, _tokenSequence.Position)),
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
                    _exceptions.Add(new ParserException("break, for, while, continue, if, return keyword", _tokenSequence.Position, _tokenSequence.Position));
                    return false;
            }
        }
        return false;
    }

    private bool TryParseConditionalExpression(out ConditionNode conditionNode)
    {
        var conditionStart = _tokenSequence.Position;
        conditionNode = null;
        if (!TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(x => x.ToKeyword() == Keywords.If,
                () => _exceptions.Add(new ParserException("if", _tokenSequence.Position, _tokenSequence.Position)),
                AddUnexpectedToken<Word>, out _))
        {
            return false;
        }

        var isConditionParsed = TryParseConditionClause(out var baseClause);
        var elifClauses = new List<ClauseNode>();
        Word word;
        while (TryParseEmpty(out _) || TryParseScopedWithDepth(TryParseElifKeyword, out word))
        {
            var clauseStart = _tokenSequence.Position;
            if (TryParseConditionClause(out var elifClause))
            {
                elifClauses.Add(elifClause);
            }
            else
            {
                _exceptions.Add(new ParserException("elif clause", clauseStart, _tokenSequence.Position));   
            }
        }

        var elseBody = default(BodyNode);
        if (word.ToKeyword() == Keywords.Else)
        {
            AdvanceToEndOfLineAndAddException();
            var elseStart = _tokenSequence.Position;
            if (!TryParseBody(out elseBody))
            {
                _exceptions.Add(new ParserException("else body expression", elseStart, _tokenSequence.Position));
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
        _exceptions.Add(new ParserException("a valid condition expression", conditionStart, _tokenSequence.Position));
        return false;

        bool TryParseElifKeyword(out Word res)
        {
             return TryConsumeNextNonWhiteSpace(x => x.ToKeyword() == Keywords.Elif, () => { }, out res);
        }
    }

    private bool TryParseConditionClause(out ClauseNode conditionNode)
    {
        var isPredicateParsed = TryParseInParen<NodeBase, OpenParen, CloseParen>(TryParseExpression,
            () =>
            {
                _exceptions.Add(new ParserException("conditional expression", _tokenSequence.Position,
                    _tokenSequence.Position));
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

    private bool TryParseForLoop(out ForNode forNode)
    {
        forNode = null;
        if (!TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(x => x.ToKeyword() == Keywords.For,
                () => _exceptions.Add(new ParserException("for", _tokenSequence.Position, _tokenSequence.Position)),
                AddUnexpectedToken<Word>, out _))
        {
            return false;
        }

        var isHeaderParsed = TryParseInParen<ForHeaderHolder, OpenParen, CloseParen>(TryParseForHeader, () =>
        {
            _exceptions.Add(new ParserException("for header", _tokenSequence.Position, _tokenSequence.Position));
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

    private record struct ForHeaderHolder(VariableDefinitionNode IteratorVar, NodeBase Iterable);

    private bool TryParseForHeader(out ForHeaderHolder headerHolder)
    {
        var isDefinitionParsed = TryParseCreateVariable(out var node);
        var isKeywordParsed = TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.In,
            () => _exceptions.Add(new ParserException("in keyword", _tokenSequence.Position, _tokenSequence.Position)),
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
    
    private bool TryParseWhileLoop(out WhileNode whileNode)
    {
        whileNode = null;
        if (!TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(x => x.ToKeyword() == Keywords.While,
                () => _exceptions.Add(new ParserException("while", _tokenSequence.Position, _tokenSequence.Position)),
                AddUnexpectedToken<Word>, out _))
        {
            return false;
        }

        var isParsedPredicate = TryParseInParen<NodeBase, OpenParen, CloseParen>(TryParseExpression, () =>
        {
            _exceptions.Add(new ParserException("conditional expression", _tokenSequence.Position,
                _tokenSequence.Position));
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

    private bool TryParseCreateVariable(out VariableDefinitionNode variableDefinitionNode)
    {
        var start = _tokenSequence.Position;
        variableDefinitionNode = null;
        var isTypeParsed = TryParseType(out var type);
        var isWordParsed = TryConsumeNextNonWhiteSpace<Word>(x => x.ToKeyword() == Keywords.Unknown,
            AddKeywordException, out var word);
        
        if (isTypeParsed && isWordParsed)
        {
            variableDefinitionNode = new VariableDefinitionNode(type, new MemberNode(word.GetString()));
            return true;
        }   
        _exceptions.Add(new ParserException("variable defenition", start, _tokenSequence.Position));
        return false;
    }

    private bool TryParseExpression(out NodeBase expression)
    {
        return TryParseWithPrecedence(out expression);
    }

    private bool TryParseWithPrecedence(out NodeBase node, int rbp = 0)
    {
        var left = ParseNud(scope, assemblyDescriptions);
        while (TryParseLed(scope, assemblyDescriptions, rbp, left, out left))
        {
        }

        return left;
    }

    private Expression ParseNud(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        if (_tokenSequence.PeekNextNonWhiteSpace() is OpenParen)
        {
            return ParseInParen<Expression, OpenParen, CloseParen>(
                () => ParseWithPrecedence(scope, assemblyDescriptions),
                () => throw new ParserException($"Empty paren pair"));
        }

        if (!TryConsumeNextNonWhiteSpace<Operator>(_ => true, out var token))
        {
            return ParsePostfixIfExist(ParseVariableConstantOrCall(scope, assemblyDescriptions));
        }

        var op = token.ToOperator();
        return op switch
        {
            OperatorEnum.Minus => new UnaryMinus(ParseWithPrecedence(scope, assemblyDescriptions,
                op.GetPrecedence(true))),
            OperatorEnum.Not =>
                new Negate(ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(true))),
            OperatorEnum.Increment => new PrefixIncrement(ParseWithPrecedence(scope, assemblyDescriptions,
                op.GetPrecedence(true))),
            OperatorEnum.Decrement => new PrefixDecrement(ParseWithPrecedence(scope, assemblyDescriptions,
                op.GetPrecedence(true))),
            _ => throw new ParserException($"Invalid operator {op} in current context")
        };

    }

    private Expression ParsePostfixIfExist(Expression inner)
    {
        if (!TryConsumeNextNonWhiteSpace<Operator>(_ => true, out var token))
        {
            return inner;
        }

        var op = token.ToOperator();
        switch (op)
        {
            case OperatorEnum.Increment:
                return new PostfixIncrement(inner);
            case OperatorEnum.Decrement:
                return new PostfixDecrement(inner);
            default:
                _tokenSequence.RollBackToNonWhiteSpace(0);
                return inner;
        }
    }

    private bool TryParseLed(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions,
        int rbp, Expression left, out Expression output)
    {
        if (TryConsumeNextNonWhiteSpace<Operator>(_ => true, out var token))
        {
            var op = token.ToOperator();
            var precedence = op.GetPrecedence(false);
            if (precedence <= rbp)
            {
                output = left;
                _tokenSequence.RollBackToNonWhiteSpace(0);
                return false;
            }

            switch (op)
            {
                case OperatorEnum.Call:
                    output
                case OperatorEnum.Multiply:
                    output = new Multiply(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case OperatorEnum.Divide:
                    output = new Divide(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case OperatorEnum.Plus:
                    output = new Plus(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case OperatorEnum.Minus:
                    output = new Minus(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case OperatorEnum.Lesser:
                    output = new Less(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case OperatorEnum.Greater:
                    output = new Greater(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case OperatorEnum.LesserOrEquals:
                    output = new LessOrEquals(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case OperatorEnum.GreaterOrEquals:
                    output = new GreaterOrEquals(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case OperatorEnum.Equals:
                    output = new Equal(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case OperatorEnum.NotEquals:
                    output = new NotEquals(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case OperatorEnum.And:
                    output = new And(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case OperatorEnum.Or:
                    output = new Or(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case OperatorEnum.Modulo:
                    output = new Modulo(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
            }
        }

        output = left;
        return false;
    }

    private Expression ParseVariableConstantOrCall(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        if (TryConsumeNextNonWhiteSpace<Word>(w => w.ToKeyword() != Keywords.Unknown, out var word))
        {
            var keyword = word.ToKeyword();
            return keyword switch
            {
                Keywords.True => new ConstantExpression(true, typeof(bool)),
                Keywords.False => new ConstantExpression(false, typeof(bool)),
                Keywords.Null => new ConstantExpression(null, typeof(void)),
                _ => throw new ParserException($"Invalid keyword usage {keyword}")
            };
        }

        if (TryConsumeNextNonWhiteSpace(x => int.TryParse(x.GetString(), out _), out word))
        {
            return new ConstantExpression(int.Parse(word.GetString()), typeof(int));
        }

        if (TryConsumeNextNonWhiteSpace(x => long.TryParse(x.GetString(), out _), out word))
        {
            return new ConstantExpression(long.Parse(word.GetString()), typeof(long));
        }

        if (TryConsumeNextNonWhiteSpace(x => double.TryParse(x.GetString(), out _), out word))
        {
            return new ConstantExpression(double.Parse(word.GetString()), typeof(double));
        }

        if (TryConsumeNextNonWhiteSpace<StringLiteral>(_ => true, out var literal))
        {
            return new ConstantExpression(literal.GetString(), typeof(string));
        }

        //TODO: переменная перебьёт вызов
        if (_tokenSequence.PeekNextNonWhiteSpace(1) is not OpenParen or Operator
            && TryConsumeNextNonWhiteSpace<Word>(w => w.ToKeyword() == Keywords.Unknown, out var token))
        {
            if (scope.TryGetVariable(token.GetString(), out var variable))
            {
                return variable;
            }

            _tokenSequence.RollBackToNonWhiteSpace();
        }

        if (TryConsumeNextNonWhiteSpace(_ => true, out token))
        {
            _tokenSequence.RollBackToNonWhiteSpace();
            return ParseCall(scope, assemblyDescriptions);
        }

        throw new ParserException($"invalid variable constant or call expresion");
    }

    private Expression ParseCtor(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        var name = _tokenSequence.PeekNextNonWhiteSpace().GetString();
        var type = ParseType(assemblyDescriptions);
        var expressions = ParseInParen<List<Expression>, OpenParen, CloseParen>(
            () => ParseCommaSeparated(
                () => ParseExpression(scope, assemblyDescriptions)),
            () => []);
        var signature = expressions.Select(x => x.GetReturnType()).ToList();
        var ctorMethod = default(MethodInfo);
        foreach (var assembly in assemblyDescriptions)
        {
            if (assembly.TryMatchSignature(name, type, signature, out var method))
            {
                //TODO: зона ответственности сборок
                if (ctorMethod != default)
                {
                    throw new ParserException($"ambugulous method invocation");
                }

                ctorMethod = method;
            }
        }

        return new CallExpression(ctorMethod, expressions);
    }

    private bool TryParseInParen<TResult, TOpen, TClose>(TryParseInternal<TResult> parserFunc, Func<TResult> emptyCase, out TResult result)
        where TOpen : TokenBase where TClose : TokenBase
    {
        result = default;
        if (!TryConsumeNextNonWhiteSpaceWithoutRollback<TOpen>(_ => true, () => { },
                () => AddUnexpectedToken(typeof(TOpen).Name), out _))
        {
            return false;
        }
        if (TryConsumeNextNonWhiteSpace<TClose>(_ => true, () => { }, out _))
        {
            result = emptyCase();
            return true;
        }

        parserFunc(out result);
        if (TryConsumeNextNonWhiteSpace<TClose>(_ => true, () => { }, out _))
        {
            return true;
        }
        var unexpectedStart = _tokenSequence.Position;
        AdvanceToFirstOfTokens([typeof(EndOfLine), typeof(TClose)]);
        _exceptions.Add(new ParserException("End of line", unexpectedStart, _tokenSequence.Position));
        return false;
    }

    private bool TryParseCommaSeparated<TReturn>(TryParseInternal<TReturn> parserFunc, out List<TReturn> result)
    {
        result = [];
        while (true)
        {
            parserFunc(out var res);
            result.Add(res);

            if (!TryConsumeNextNonWhiteSpace<Comma>(_ => true, () => {}, out _))
            {
                return true;
            }
        }
    }

    private bool TryConsumeNextNonWhiteSpace<TToken>(Func<TToken, bool> predicate, Action ifPredicateFalse, out TToken token)
        where TToken : TokenBase
    {
        var next = _tokenSequence.PeekNextNonWhiteSpace();
        if (next is TToken target && predicate(target))
        {
            token = target;
            _tokenSequence.GetNextNonWhiteSpace();
            return true;
        }

        token = null;
        return false;
    }

    private bool TryConsumeNextNonWhiteSpaceWithoutRollback<TToken>(Func<TToken, bool> predicate, 
        Action ifPredicateFalse, Action ifTokenMismatch, out TToken token)
        where TToken : TokenBase
    {
        token = null;
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

    private void RollBackToRequestedNonWhiteSpaceToken<T>() where T : TokenBase
    {
        while (true)
        {
            var token = _tokenSequence.RollBackToNonWhiteSpace();
            if (token is null || token.GetType() == typeof(T))
            {
                return;
            }
        }
    }

    private void AdvanceToRequestedToken<T>() where T : TokenBase
    {
        while (true)
        {
            var token = _tokenSequence.PeekNext();
            if (token == null)
            {
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

    private void AdvanceToFirstOfTokens(List<Type> tokenTypes)
    {
        while (true)
        {
            var token = _tokenSequence.PeekNext();
            if (token == null)
            {
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
    
    private void AddKeywordException() =>
        _exceptions.Add(new ParserException("non keyword", _tokenSequence.Position, _tokenSequence.Position));

    private void AddUnexpectedToken(string expected) =>
        _exceptions.Add(new ParserException(expected, _tokenSequence.Position, _tokenSequence.Position));

    private TryParseInternal<List<TReturn>> WrapParseCommaSeparated<TReturn>(TryParseInternal<TReturn> parserFunc)
    {
        return FuncWrapper;
        bool FuncWrapper(out List<TReturn> resultList)
        {
            return TryParseCommaSeparated(parserFunc, out resultList);
        }
    }
    
    private void AddUnexpectedToken<T>() where T : TokenBase =>
        _exceptions.Add(new ParserException(typeof(T).Name, _tokenSequence.Position, _tokenSequence.Position));

    private void AdvanceToEndOfLineAndAddException()
    {
        if (_tokenSequence.Current().GetType() == typeof(EndOfLine))
        {
            return;
        }
        var unexpectedStart = _tokenSequence.Position;
        AdvanceToRequestedToken<EndOfLine>();
        _exceptions.Add(new ParserException("End of line", unexpectedStart, _tokenSequence.Position));
    }
}