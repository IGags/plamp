using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Extensions.Ast.Comparers;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser;

public class ParseDefTests
{
    private static readonly ExtendedRecursiveComparer Comparer = new();
    
    [Fact]
    public void ParseValidDefSingleLine()
    {
        const string code = """
                            def int notify() return 1
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new DefNode(
                new TypeNode(
                    new MemberNode("int"),
                    null),
                new MemberNode("notify"),
                [
                ],
                new BodyNode(
                [
                    new ReturnNode(
                        new LiteralNode(1, typeof(int)))
                ]));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(11, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseValidDefMultiLine()
    {
        const string code = """
                            def int notify()
                                var a=1*2
                                return a
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new DefNode(
                new TypeNode(
                    new MemberNode("int"),
                    null),
                new MemberNode("notify"),
                [],
                new BodyNode(
                [
                    new AssignNode(
                        new VariableDefinitionNode(
                            null, new MemberNode("a")),
                        new MultiplyNode(
                            new LiteralNode(1, typeof(int)),
                            new LiteralNode(2, typeof(int)))),
                    new ReturnNode(
                        new MemberNode("a"))
                ]));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(21, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseValidDefSingleLineWithArgs()
    {
        const string code = """
                            def void add(List<int> list, int arg) list.add(arg)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new DefNode(
                new TypeNode(new MemberNode("void"), null),
                new MemberNode("add"),
                [
                    new ParameterNode(
                        new TypeNode(
                            new MemberNode("List"),
                            [
                                new TypeNode(
                                    new MemberNode("int"),
                                    null)
                            ]),
                        new MemberNode("list")),
                    new ParameterNode(
                        new TypeNode(
                            new MemberNode("int"),
                            null),
                        new MemberNode("arg"))
                ],
                new BodyNode(
                [
                    new CallNode(
                        new MemberNode("list"),
                        new MemberNode("add"),
                        [
                            new MemberNode("arg")
                        ])
                ]));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(25, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseDefWithoutBody()
    {
        const string code = """
                            def void a()
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new DefNode(
                new TypeNode(
                    new MemberNode("void"),
                    null),
                new MemberNode("a"),
                [],
                new BodyNode(
                    []));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(7, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseInvalidBetweenTypeAndDef()
    {
        const string code = """
                            def + void a()
                                return 1
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(14, context.TokenSequence.Position);
        Assert.Equal(3, context.TransactionSource.Exceptions.Count);
        var exceptionShould1 = new PlampException(
            PlampNativeExceptionInfo.InvalidDefMissingReturnType(),
            new(0, 0), new(0, 2),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould1, context.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 3), new(0, 15),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould2, context.TransactionSource.Exceptions[1]);
        var exceptionShould3 = new PlampException(
            PlampNativeExceptionInfo.InvalidBody(),
            new(1, 4), new(1, 13),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould3, context.TransactionSource.Exceptions[2]);
    }

    [Fact]
    public void ParseInvalidBetweenTypeAndName()
    {
        const string code = """
                            def void + a() return 1
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(13, context.TokenSequence.Position);
        Assert.Equal(2, context.TransactionSource.Exceptions.Count);
        var exceptionShould1 = new PlampException(
            PlampNativeExceptionInfo.InvalidDefMissingName(),
            new(0, 0), new(0, 2),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould1, context.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 8), new(0, 24),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould2, context.TransactionSource.Exceptions[1]);
    }

    [Fact]
    public void ParseInvalidBetweenNameAndOpenParen()
    {
        //TODO: Improve parser recovery
        const string code = """
                            def void a+(int aa) return 1
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new DefNode(
                new TypeNode(
                    new MemberNode("void"),
                    null),
                new MemberNode("a"),
                [],
                new BodyNode(
                    []));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(15, context.TokenSequence.Position);
        Assert.Equal(2, context.TransactionSource.Exceptions.Count);
        var exceptionShould1 = new PlampException(
            PlampNativeExceptionInfo.ExpectedArgDefinition(),
            new(0, 0), new(0, 2),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould1, context.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 10), new(0, 29),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould2, context.TransactionSource.Exceptions[1]);
    }

    [Fact]
    public void ParseInvalidBetweenBetweenTypeAndBody()
    {
        const string code = """
                            def void a() +return 1
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new DefNode(
                new TypeNode(
                    new MemberNode("void"),
                    null),
                new MemberNode("a"),
                [],
                new BodyNode(
                    []));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(12, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 12), new(0, 23),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }

    #region Symbol talbe

    [Fact]
    public void DefWithoutBody()
    {
        const string code = "def void print()";

        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(5, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[0], symbol.Tokens[0]);
        Assert.Equal(3, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    [Fact]
    public void DefWithBody()
    {
        const string code = "def void print() bible.write(\"god was die\")";

        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(9, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[0], symbol.Tokens[0]);
        Assert.Equal(3, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    [Fact]
    public void DefWithArgs()
    {
        const string code = "def void print(string str)";
        
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(9, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[0], symbol.Tokens[0]);
        Assert.Equal(4, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }
    
    #endregion
}