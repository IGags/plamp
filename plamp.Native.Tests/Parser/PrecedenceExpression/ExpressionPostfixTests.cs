using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.NodeComparers;
using plamp.Native.Parsing;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.PrecedenceExpression;

public class ExpressionPostfixTests
{
    private static readonly RecursiveComparer Comparer = new();
    
    [Fact]
    public void ParseSingleMember()
    {
        const string code = "hi";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould = new MemberNode("hi");
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(0, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseMemberAccess()
    {
        const string code = "hey.dude";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new MemberAccessNode(
                new MemberNode("hey"), 
                new MemberNode("dude"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(2, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseMemberAccessSequence()
    {
        const string code = "hey.dude.you.cool";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new MemberAccessNode(
                new MemberAccessNode(
                    new MemberAccessNode(
                        new MemberNode("hey"),
                        new MemberNode("dude")),
                    new MemberNode("you")),
                new MemberNode("cool"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(6, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }
    
    [Fact]
    public void ParseAccessWithoutNextMember()
    {
        const string code = "hey.";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.Equal(new MemberNode("hey"), expression, Comparer);
        Assert.Equal(0, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseCallExpression()
    {
        const string code = "greet()";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new CallNode(
                new MemberNode("greet"),
                []);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(2, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseInvalidCallExpression()
    {
        const string code = "greet.()";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould = new MemberNode("greet");
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(0, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseCallWithArgs()
    {
        const string code = "greet(\"hi\"+\"you\", 1)";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new CallNode(
                new MemberNode("greet"),
                [
                    new PlusNode(
                        new LiteralNode("hi", typeof(string)),
                        new LiteralNode("you", typeof(string))),
                    new LiteralNode(1, typeof(int))
                ]);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(8, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseCallSequence()
    {
        const string code = "arg.greet(greeter).bye()";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new CallNode(
                new MemberAccessNode(
                    new CallNode(
                        new MemberAccessNode(
                            new MemberNode("arg"),
                            new MemberNode("greet")),
                        [
                            new MemberNode("greeter")
                        ]),
                    new MemberNode("bye")),
                []);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(9, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseIndexer()
    {
        const string code = "arr[0]";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new IndexerNode(
                new MemberNode("arr"),
                [
                    new LiteralNode(0, typeof(int))
                ]);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(3, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseMultidimensionalIndexer()
    {
        const string code = "arr[0,1]";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new IndexerNode(
                new MemberNode("arr"),
                [
                    new LiteralNode(0, typeof(int)),
                    new LiteralNode(1, typeof(int))
                ]);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(5, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseIndexerSequence()
    {
        const string code = "arr[0][\"hi\"]";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new IndexerNode(
                new IndexerNode(
                    new MemberNode("arr"),
                    [
                        new LiteralNode(0, typeof(int))
                    ]),
                [
                    new LiteralNode("hi", typeof(string))
                ]);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(6, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    #region Symbol table

    [Fact]
    public void MemberSymbolTest()
    {
        const string code = """
                            mem
                            """;

        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);

        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Single(symbolTable);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[0], symbol.Tokens[0]);
        Assert.Empty(symbol.Children);
    }

    [Fact]
    public void MemberAccessSequenceSymbolTest()
    {
        const string code = """
                            mem.d
                            """;

        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);

        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(3, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[1], symbol.Tokens[0]);
        Assert.Equal(2, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    [Fact]
    public void CallSymbolTest()
    {
        const string code = """
                            mem()
                            """;

        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);

        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(2, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Empty(symbol.Tokens);
        Assert.Equal(1, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    [Fact]
    public void CallWithArgSymbolTest()
    {
        const string code = """
                            mem(m1, m2)
                            """;

        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);

        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(4, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Empty(symbol.Tokens);
        Assert.Equal(3, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    [Fact]
    public void CallSequenceTest()
    {
        const string code = """
                            mem(m1, m2).mem(t)
                            """;

        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);

        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(8, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Empty(symbol.Tokens);
        Assert.Equal(2, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    [Fact]
    public void IndexerSymbolTest()
    {
        const string code = """
                            r[2]
                            """;

        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(3, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Empty(symbol.Tokens);
        Assert.Equal(2, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    [Fact]
    public void MultidimensionalIndexerSymbolTest()
    {
        const string code = """
                            canon[1,0]
                            """;

        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(4, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Empty(symbol.Tokens);
        Assert.Equal(3, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    [Fact]
    public void IndexerSequenceSymbolTest()
    {
        const string code = """
                            canon[1][0]
                            """;
        
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(5, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Empty(symbol.Tokens);
        Assert.Equal(2, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    #endregion
}