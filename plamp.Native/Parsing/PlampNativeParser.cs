using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Body;
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
            TryParseInParen<List<ParameterNode>, OpenBracket, CloseBracket>(
                WrapParseCommaSeparated<ParameterNode>(TryParseParameter),
                () => [], out var parameterNodes);
        if (!TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, () => { }, out _))
        {
            var unexpectedStart = _tokenSequence.Position;
            AdvanceToRequestedToken<EndOfLine>();
            _exceptions.Add(new ParserException("End of line", unexpectedStart, _tokenSequence.Position));
        }
        
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
        while (TryParseScopedWithDepth(TryParseSingleLineExpression, out var expression))
        {
            expressions.Add(expression);
            if (!TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, () => { }, out _))
            {
                var unexpectedStart = _tokenSequence.Position;
                AdvanceToRequestedToken<EndOfLine>();
                _exceptions.Add(new ParserException("End of line", unexpectedStart, _tokenSequence.Position));
            }
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
        var start = _tokenSequence.Current();
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

    private Expression ParseSingleLineExpression(VariableScope scope, List<IAssemblyDescription> assemblyDescriptions)
    {
        if (_tokenSequence.PeekNextNonWhiteSpace() is Word word
            && word.ToKeyword() != Keywords.Unknown)
        {
            return ParseKeywordExpression(scope, assemblyDescriptions);
        }

        if (_tokenSequence.PeekNextNonWhiteSpace(2) is Operator op
            && op.ToOperator() == OperatorEnum.Assign)
        {
            return ParseCreateVariableAndAssign(scope, assemblyDescriptions);
        }

        if (_tokenSequence.PeekNextNonWhiteSpace(1) is Operator @operator
            && (@operator.ToOperator() == OperatorEnum.Assign
                || @operator.ToOperator() == OperatorEnum.PlusAndAssign
                || @operator.ToOperator() == OperatorEnum.MinusAndAssign
                || @operator.ToOperator() == OperatorEnum.MultiplyAndAssign
                || @operator.ToOperator() == OperatorEnum.DivideAndAssign))
        {
            return ParseAssign(scope, assemblyDescriptions);
        }

        //TODO: адекватная ошибка
        return ParseCall(scope, assemblyDescriptions);
    }

    private Expression ParseKeywordExpression(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        var word = ParseNonWhiteSpaceWithException<Word>(_ => true).ToKeyword();
        return word switch
        {
            Keywords.Break => new BreakExpression(),
            Keywords.Return => new ReturnExpression(ParseExpression(scope, assemblyDescriptions)),
            Keywords.Continue => new ContinueExpression(),
            Keywords.If => ParseConditionalExpression(scope, assemblyDescriptions),
            Keywords.For => ParseForLoop(scope, assemblyDescriptions),
            Keywords.While => ParseWhileLoop(scope, assemblyDescriptions),
            _ => throw new ParserException($"unexpected keyword {word}")
        };
    }

    private Expression ParseConditionalExpression(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        var baseClause = ParseConditionClause(scope, assemblyDescriptions);
        var elifClauses = new List<ClauseExpression>();

        //TODO кривое употребление кляуз
        while (TryParseScopedWithDepth(
                   sc =>
                       TryConsumeNextNonWhiteSpace<Word>(w => w.ToKeyword() == Keywords.Elif, out var word),
                   scope, out var res) && res)
        {
            var clause = ParseConditionClause(scope, assemblyDescriptions);
            elifClauses.Add(clause);
        }

        var elseBody = default(BodyExpression);
        if (TryConsumeNextNonWhiteSpace<Word>(w => w.ToKeyword() == Keywords.Else, out _))
        {
            ParseNonWhiteSpaceWithException<EndOfLine>(_ => true);
            elseBody = ParseBody(scope, assemblyDescriptions);
        }

        _tokenSequence.RollBackToNonWhiteSpace();

        return new ConditionExpression(baseClause, elifClauses, elseBody);
    }

    private ClauseExpression ParseConditionClause(VariableScope scope, List<IAssemblyDescription> assemblyDescriptions)
    {
        var baseCondition = ParseInParen<Expression, OpenBracket, CloseBracket>(
            () => ParseExpression(scope, assemblyDescriptions),
            () => throw new ParserException("empty condition block"));
        ParseNonWhiteSpaceWithException<EndOfLine>(_ => true);
        using var child = scope.Enter();
        var baseBody = ParseBody(scope, assemblyDescriptions);
        var baseClause = new ClauseExpression(baseCondition, baseBody);
        return baseClause;
    }

    private Expression ParseForLoop(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        using var child = scope.Enter();
        var holder = ParseInParen<ForHeaderHolder, OpenBracket, CloseBracket>(
            () =>
            {
                var iteratorVar = ParseCreateVariable(child, assemblyDescriptions);
                ParseNonWhiteSpaceWithException<Word>(w => w.ToKeyword() == Keywords.In);
                var expression = ParseExpression(child, assemblyDescriptions);
                return new ForHeaderHolder(iteratorVar, expression);
            },
            () => throw new ParserException("invalid for loop header"));
        ParseNonWhiteSpaceWithException<EndOfLine>(_ => true);
        var body = ParseBody(child, assemblyDescriptions);
        return new ForExpression(holder.IteratorVar, holder.Iterable, body);
    }

    private record struct ForHeaderHolder(CreateVariableExpression IteratorVar, Expression Iterable);

    private Expression ParseWhileLoop(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        var predicate = ParseInParen<Expression, OpenBracket, CloseBracket>(
            () => ParseExpression(scope, assemblyDescriptions),
            () => throw new ParserException($"invalid while predicate"));
        using var child = scope.Enter();
        var body = ParseBody(scope, assemblyDescriptions);
        return new WhileExpression(predicate, body);
    }

    private Expression ParseCreateVariableAndAssign(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        var createVariable = ParseCreateVariable(scope, assemblyDescriptions);
        var op = ParseNonWhiteSpaceWithException<Operator>(_ => true);
        var right = ParseExpression(scope, assemblyDescriptions);
        if (op.ToOperator() == OperatorEnum.Assign)
        {
            return new AssignNode(createVariable, right);
        }

        throw new ParserException("invalid create variable and assign expression");
    }

    private Expression ParseAssign(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        var variable = ParseNonWhiteSpaceWithException<Word>(_ => true);
        var definition = scope.GetVariable(variable.GetString());
        var op = ParseNonWhiteSpaceWithException<Operator>(_ => true);
        var right = ParseExpression(scope, assemblyDescriptions);
        return op.ToOperator() switch
        {
            OperatorEnum.Assign => new AssignNode(definition, right),
            OperatorEnum.PlusAndAssign => new AddAndAssignExpression(definition, right),
            OperatorEnum.MinusAndAssign => new SubAndAssignExpression(definition, right),
            OperatorEnum.MultiplyAndAssign => new MulAndAssignExpression(definition, right),
            OperatorEnum.DivideAndAssign => new DivAndAssignExpression(definition, right),
            OperatorEnum.ModuloAndAssign => new ModuloAndAssign(definition, right),
            _ => throw new ParserException($"invalid assign expression")
        };
    }

    private CreateVariableExpression ParseCreateVariable(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        var type = ParseType(assemblyDescriptions);
        var variable = ParseNonWhiteSpaceWithException<Word>(_ => true);
        var variableDefinition = new VariableDefinition(type, variable.GetString());
        var createVariable = new CreateVariableExpression(variableDefinition);
        scope.AddVariable(createVariable);
        return createVariable;
    }

    private Expression ParseCall(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        //TODO: метод чейнинг
        if (TryParseDotSeparatedSequence(out var definition, 3))
        {
            var assembly = assemblyDescriptions.First(x => x.Name == definition[0].GetString());
            //TODO: спиздил из ктора, это плохо
            var expressions = ParseInParen<List<Expression>, OpenBracket, CloseBracket>(
                () => ParseCommaSeparated(
                    () => ParseExpression(scope, assemblyDescriptions)),
                () => []);
            var signature = expressions.Select(x => x.GetReturnType()).ToList();
            //TODO: вся валидация в отдельном пакете
            if (!assembly.TryMatchSignature(definition[2].GetString(), null, signature, out var methodInfo))
            {
                throw new ParserException($"Unexpected method {definition[2].GetString()}");
            }

            return new CallExpression(methodInfo, expressions);
        }

        if (TryParseDotSeparatedSequence(out definition, 2))
        {
            if (scope.TryGetVariable(definition[0].GetString(), out var variable))
            {
                //TODO: вызовы методов из переменных
                throw new NotImplementedException();
            }

            var expressions = ParseInParen<List<Expression>, OpenBracket, CloseBracket>(
                () => ParseCommaSeparated(
                    () => ParseExpression(scope, assemblyDescriptions)),
                () => []);
            var signature = expressions.Select(x => x.GetReturnType()).ToList();
            var ctorMethod = default(MethodInfo);
            foreach (var assembly in assemblyDescriptions)
            {
                if (assembly.TryMatchSignature(definition[1].GetString(), null, signature, out var method))
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

        if (TryParseDotSeparatedSequence(out definition, 1))
        {
            //Вызовы методов из текщей сборки
            throw new NotImplementedException();
        }

        throw new ParserException("Not method call");
        //variable.method
        //Type.method
        //asm.Type.method
    }

    private bool TryParseDotSeparatedSequence(out Word[] tokenList, int length)
    {
        tokenList = new Word[length];
        var counter = -1;
        for (var i = 0; i < length; i++)
        {
            if (TryConsumeNextNonWhiteSpace<Word>(_ => true, out var token))
            {
                tokenList[i] = token;
                counter++;
                if (i + 1 == length)
                {
                    return true;
                }
            }
            else if (i > 0)
            {
                _tokenSequence.RollBackToNonWhiteSpace(counter);
                return false;
            }
            else
            {
                return false;
            }

            if (TryConsumeNextNonWhiteSpace<Operator>(op => op.ToOperator() == OperatorEnum.Call, out var op))
            {
                counter++;
                continue;
            }

            if (i <= 0)
            {
                return false;
            }

            _tokenSequence.RollBackToNonWhiteSpace(counter);
            return false;

        }

        return false;
    }

    private Expression ParseExpression(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        if (TryConsumeNextNonWhiteSpace<Word>(w => w.ToKeyword() == Keywords.New, out _))
        {
            return ParseCtor(scope, assemblyDescriptions);
        }

        return ParseWithPrecedence(scope, assemblyDescriptions);
    }

    //TODO: кривой приоритет операторов
    private Expression ParseWithPrecedence(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions,
        int rbp = 0)
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
        if (_tokenSequence.PeekNextNonWhiteSpace() is OpenBracket)
        {
            return ParseInParen<Expression, OpenBracket, CloseBracket>(
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
        if (_tokenSequence.PeekNextNonWhiteSpace(1) is not OpenBracket or Operator
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
        var expressions = ParseInParen<List<Expression>, OpenBracket, CloseBracket>(
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

    private TToken TryParseNonWhiteSpaceWithException<TToken>(Func<TToken, bool> predicate)
    {
        var token = _tokenSequence.GetNextNonWhiteSpace();
        if (token is not TToken typedToken || !predicate(typedToken))
        {
            throw new ParserException(token, typeof(TToken).Name);
        }

        return typedToken;
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
        return TryConsumeNextNonWhiteSpaceWithoutRollback<TClose>(_ => true, () => { },
            () => AddUnexpectedToken(typeof(TClose).Name), out _);
    }

    private bool TryParseCommaSeparated<TReturn>(TryParseInternal<TReturn> parserFunc, out List<TReturn> result)
    {
        result = [];
        while (true)
        {
            parserFunc(out var res);
            result.Add(res);

            if (!TryConsumeNextNonWhiteSpace<Comma>(_ => true, out _))
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
}