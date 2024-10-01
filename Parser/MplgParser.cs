using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Parser.Assembly;
using Parser.Ast;
using Parser.Token;
using ConstantExpression = Parser.Ast.ConstantExpression;
using Expression = Parser.Ast.Expression;
using Operator = Parser.Token.Operator;

namespace Parser;

public class MplgParser
{
    private readonly TokenSequence _tokenSequence;
    
    public MplgParser(string code)
    {
        _tokenSequence = code.Tokenize();
    }
    
    //TODO: Несколько проходок парсером для независимости сигнатур компонентов
    public List<FuncExpression> Parse(List<IAssemblyDescription> assemblies)
    {
        var expressionList = new List<FuncExpression>();
        
        while (_tokenSequence.Current() != null)
        {
            ParseTopLevel(expressionList, assemblies);
        }

        return expressionList;
    }

    //TODO: multi iteration parsing
    //TODO: ambiguous assemblies
    private void ParseTopLevel(List<FuncExpression> expressions, List<IAssemblyDescription> assemblyDescriptions)
    {
        var token = _tokenSequence.GetNextNonWhiteSpace();
        if (token is Word word)
        {
            switch (word.ToKeyword())
            {
                case Keywords.Def:
                    ParseFunction(expressions, assemblyDescriptions);
                    return;
                case Keywords.Use:
                    ParseUsing(assemblyDescriptions);
                    return;
            }
                
        }
        throw new ParserException(token, "use or def keyword");
    }

    //TODO: using to expression tree
    private void ParseUsing(List<IAssemblyDescription> assemblyDescriptions)
    {
        throw new NotImplementedException();
    }
    
    private void ParseFunction(List<FuncExpression> expressions, List<IAssemblyDescription> assemblyDescriptions)
    {
        using var scope = new VariableScope(null);
        var returnType = new TypeDescription(ParseType(assemblyDescriptions));
        var wordToken = ParseNonWhiteSpaceWithException<Word>(_ => true);

        var functionName = wordToken.GetString();
        var args = ParseInParen<List<ParameterDescription>, OpenBracket, CloseBracket>(
            () => ParseCommaSeparated(() => ParseParameter(assemblyDescriptions)),
            () => []);
        args.ForEach(x => scope.AddVariable(new VariableDefinition(x.TypeName, x.Name)));
        ParseNonWhiteSpaceWithException<EOF>(_ => true);
        var body = ParseBody(scope, assemblyDescriptions);
        var expression = new FuncExpression(functionName, returnType, args.ToArray(), body);
        expressions.Add(expression);
    }

    private BodyExpression ParseBody(VariableScope scope, List<IAssemblyDescription> assemblyDescriptions)
    {
        using var inner = scope.Enter();
        var expressions = new List<Expression>();
        while (TryParseScopedWithDepth(innerScope => ParseSingleLineExpression(innerScope, assemblyDescriptions), inner, out var expression))
        {
            expressions.Add(expression);
            ParseNonWhiteSpaceWithException<EOF>(_ => true);
        }

        var body = new BodyExpression(expressions);
        return body;
    }

    private ParameterDescription ParseParameter(List<IAssemblyDescription> assemblyDescriptions)
    {
        var type = new TypeDescription(ParseType(assemblyDescriptions));

        var nameToken = ParseNonWhiteSpaceWithException<Word>(_ => true);
        //TODO: валидация имени параметра
        return new ParameterDescription(type, nameToken.GetString());
    }
    
    //TODO: более детальные ошибки парсинга
    private Type ParseType(List<IAssemblyDescription> assemblyDescriptions)
    {
        //TODO: сделать чистый текст ошибки
        var word = ParseNonWhiteSpaceWithException<Word>(w => w.ToKeyword() == Keywords.Unknown);

        var type = assemblyDescriptions.SelectMany(x => x.TypeMap)
            .FirstOrDefault(x => x.Value == word.GetString()).Key;
        if (type == null)
        {
            throw new UnexistingTypeException(word.GetString());
        }

        if (_tokenSequence.PeekNextNonWhiteSpace() is not OpenSquareBracket)
        {
            return type;
        }
        
        if (!type.IsGenericType)
        {
            throw new InvalidGenericTypeException($"the type {word.GetString()} isn't actually generic");
        }
        var innerArgs = ParseInParen<List<Type>, OpenSquareBracket, CloseSquareBracket>(
            () => ParseCommaSeparated(() => ParseType(assemblyDescriptions)),
            () => []);
        try
        {
            var completeGeneric = type.MakeGenericType(innerArgs.ToArray());
            return completeGeneric;
        }
        catch (Exception e)
        {
            throw new InvalidGenericTypeException(
                $"the number or order of generic type arguments is invalid for type: {type}");
        }
    }

    //TODO: свободное погружение в скоупы
    private bool TryParseScopedWithDepth<TReturn>(Func<VariableScope, TReturn> parserFunc, VariableScope currentScope, out TReturn result)
    {
        var currentDepth = 0;
        while (_tokenSequence.PeekNext() is Scope)
        {
            currentDepth++;
            _tokenSequence.GetNextToken();
        }

        if (currentDepth != currentScope.Depth)
        {
            _tokenSequence.RollBack(currentDepth);
            result = default;
            return false;
        }

        result = parserFunc(currentScope);
        return true;
    }

    private Expression ParseSingleLineExpression(VariableScope scope, List<IAssemblyDescription> assemblyDescriptions)
    {
        if (_tokenSequence.PeekNextNonWhiteSpace() is Word word
            && word.ToKeyword() != Keywords.Unknown)
        {
            return ParseKeywordExpression(scope, assemblyDescriptions);
        }
        
        if (_tokenSequence.PeekNextNonWhiteSpace(2) is Operator op 
            && op.ToOperator() == Ast.Operator.Assign)
        {
            return ParseCreateVariableAndAssign(scope, assemblyDescriptions);
        }

        if (_tokenSequence.PeekNextNonWhiteSpace(1) is Operator @operator
            && (@operator.ToOperator() == Ast.Operator.Assign
                || @operator.ToOperator() == Ast.Operator.PlusAndAssign
                || @operator.ToOperator() == Ast.Operator.MinusAndAssign
                || @operator.ToOperator() == Ast.Operator.MultiplyAndAssign
                || @operator.ToOperator() == Ast.Operator.DivideAndAssign))
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
        while (TryConsumeNextNonWhiteSpace<Word>(w => w.ToKeyword() == Keywords.Elif, out var word))
        {
            var clause = ParseConditionClause(scope, assemblyDescriptions);
            elifClauses.Add(clause);
        }

        var elseBody = default(BodyExpression);
        if (TryConsumeNextNonWhiteSpace<Word>(w => w.ToKeyword() == Keywords.Else, out _))
        {
            using var child = scope.Enter();
            elseBody = ParseBody(child, assemblyDescriptions);
        }

        return new ConditionExpression(baseClause, elifClauses, elseBody);
    }

    private ClauseExpression ParseConditionClause(VariableScope scope, List<IAssemblyDescription> assemblyDescriptions)
    {
        var baseCondition = ParseInParen<Expression, OpenBracket, CloseBracket>(
            () => ParseExpression(scope, assemblyDescriptions),
            () => throw new ParserException("empty condition block"));
        ParseNonWhiteSpaceWithException<EOF>(_ => true);
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
        ParseNonWhiteSpaceWithException<EOF>(_ => true);
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
        if (op.ToOperator() == Ast.Operator.Assign)
        {
            return new AssignExpression(createVariable, right);
        }

        throw new ParserException("invalid create variable and assign expression");
    }
    
    private Expression ParseAssign(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        var variable = ParseNonWhiteSpaceWithException<Word>(_ => true);
        var definition = scope.GetVariable(variable.GetString());
        var varExpression = new VariableExpression(definition);
        var op = ParseNonWhiteSpaceWithException<Operator>(_ => true);
        var right = ParseExpression(scope, assemblyDescriptions);
        return op.ToOperator() switch
        {
            Ast.Operator.Assign => new AssignExpression(varExpression, right),
            Ast.Operator.PlusAndAssign => new AddAndAssignExpression(varExpression, right),
            Ast.Operator.MinusAndAssign => new SubAndAssignExpression(varExpression, right),
            Ast.Operator.MultiplyAndAssign => new MulAndAssignExpression(varExpression, right),
            Ast.Operator.DivideAndAssign => new DivAndAssignExpression(varExpression, right),
            _ => throw new ParserException($"invalid assign expression")
        };
    }

    private CreateVariableExpression ParseCreateVariable(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        var type = ParseType(assemblyDescriptions);
        var variable = ParseNonWhiteSpaceWithException<Word>(_ => true);
        var variableDefinition = new VariableDefinition(new TypeDescription(type), variable.GetString());
        scope.AddVariable(variableDefinition);
        var createVariable = new CreateVariableExpression(variableDefinition);
        return createVariable;
    }
    
    private Expression ParseCall(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions)
    {
        
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

    
    private Expression ParseWithPrecedence(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions, 
        int rbp = 0)
    {
        var left = ParseNud(scope, assemblyDescriptions);
        while (TryParseLed(scope, assemblyDescriptions, rbp, left, out left))
        { }

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
            Ast.Operator.Minus => new UnaryMinus(ParseWithPrecedence(scope, assemblyDescriptions,
                op.GetPrecedence(true))),
            Ast.Operator.Not =>
                new Negate(ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(true))),
            Ast.Operator.Increment => new PrefixIncrement(ParseWithPrecedence(scope, assemblyDescriptions,
                op.GetPrecedence(true))),
            Ast.Operator.Decrement => new PrefixDecrement(ParseWithPrecedence(scope, assemblyDescriptions,
                op.GetPrecedence(true))),
            _ => throw new ParserException($"Invalid operator {op} in current context")
        };

    }

    private Expression ParsePostfixIfExist(Expression inner)
    {
        if (TryConsumeNextNonWhiteSpace<Operator>(_ => true, out var token))
        {
            var op = token.ToOperator();
            return op switch
            {
                Ast.Operator.Increment => new PostfixIncrement(inner),
                Ast.Operator.Decrement => new PostfixDecrement(inner)
            };
        }

        return inner;
    }
    
    private bool TryParseLed(VariableScope scope,
        List<IAssemblyDescription> assemblyDescriptions,
        int rbp, Expression left, out Expression output)
    {
        if (TryConsumeNextNonWhiteSpace<Operator>(_ => true, out var token))
        {
            var op = token.ToOperator();
            switch (op)
            {
                case Ast.Operator.Multiply:
                    output = new Multiply(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case Ast.Operator.Divide:
                    output = new Divide(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case Ast.Operator.Plus:
                    output = new Plus(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case Ast.Operator.Minus:
                    output = new Minus(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case Ast.Operator.Lesser:
                    output = new Less(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case Ast.Operator.Greater:
                    output = new Greater(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case Ast.Operator.LesserOrEquals:
                    output = new LessOrEquals(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case Ast.Operator.GreaterOrEquals:
                    output = new GreaterOrEquals(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case Ast.Operator.Equals:
                    output = new Equals(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case Ast.Operator.NotEquals:
                    output = new NotEquals(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case Ast.Operator.And:
                    output = new And(left,
                        ParseWithPrecedence(scope, assemblyDescriptions, op.GetPrecedence(false)));
                    return true;
                case Ast.Operator.Or:
                    output = new Or(left,
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

        if (TryConsumeNextNonWhiteSpace<StringLiteral>(_ => true, out var literal))
        {
            return new ConstantExpression(literal.GetString(), typeof(string));
        }
        
        if (_tokenSequence.PeekNextNonWhiteSpace(1) is not OpenBracket or Operator
            && TryConsumeNextNonWhiteSpace<Word>(w => w.ToKeyword() == Keywords.Unknown, out var token))
        {
            var variable = scope.GetVariable(token.GetString());
            return new VariableExpression(variable);
        }

        if (TryConsumeNextNonWhiteSpace(_ => true, out token))
        {
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

    private TToken ParseNonWhiteSpaceWithException<TToken>(Func<TToken, bool> predicate)
    {
        var token = _tokenSequence.GetNextNonWhiteSpace();
        if (token is not TToken typedToken || predicate(typedToken))
        {
            throw new ParserException(token, typeof(TToken).Name);
        }

        return typedToken;
    }

    private TResult ParseInParen<TResult, TOpen, TClose>(Func<TResult> parserFunc, Func<TResult> emptyCase)
        where TOpen : TokenBase where TClose : TokenBase
    {
        ParseNonWhiteSpaceWithException<TOpen>(_ => true);
        if (TryConsumeNextNonWhiteSpace<TClose>(_ => true, out _))
        {
            return emptyCase();
        }
        var result = parserFunc();
        ParseNonWhiteSpaceWithException<TClose>(_ => true);
        return result;
    }
    
    private List<TReturn> ParseCommaSeparated<TReturn>(Func<TReturn> parserFunc) 
    {
        _tokenSequence.GetNextToken();
        var result = new List<TReturn>();
        while (true)
        {
            
            var res = parserFunc();
            result.Add(res);

            if (!TryConsumeNextNonWhiteSpace<Comma>(_ => true, out _))
            {
                return result;
            }
        }
    }

    private bool TryConsumeNextNonWhiteSpace<TToken>(Func<TToken, bool> predicate, out TToken token) where TToken : TokenBase
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
}