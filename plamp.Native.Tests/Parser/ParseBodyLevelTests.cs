using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.NodeComparers;
using plamp.Native.Parsing;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser;

public class ParseBodyLevelTests
{
    private static readonly RecursiveComparer Comparer = new();
    
    //Method does not advance to end of line
    [Fact]
    public void ParseValidSingleLineExpression()
    {
        const string code = """
                            a+b-c
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseBodyLevelExpression(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new MinusNode(
                new PlusNode(
                    new MemberNode("a"),
                    new MemberNode("b")),
                new MemberNode("c"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(4, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseValidSingleLineWithTrashAfter()
    {
        const string code = """
                            a+b-c 1
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseBodyLevelExpression(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new MinusNode(
                new PlusNode(
                    new MemberNode("a"),
                    new MemberNode("b")),
                new MemberNode("c"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(4, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    //But keyword expression advance to end of line
    [Fact]
    public void ParseSmthKeywordSingleLine()
    {
        const string code = """
                            return 2 + 2 ->
                                    * 2
                            """;
        var context = ParserTestHelper.GetContext(code);
        //HoW does dis works? watch commit
        var result = PlampNativeParser.TryParseBodyLevelExpression(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ReturnNode(
                new PlusNode(
                    new LiteralNode(2, typeof(int)),
                    new MultiplyNode(
                        new LiteralNode(2, typeof(int)),
                        new LiteralNode(2, typeof(int)))
                    ));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(15, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseSmthKeywordMultiLine()
    {
        const string code = """
                            if(true)
                                print(2)
                                print(3)
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseBodyLevelExpression(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConditionNode(
                new ClauseNode(
                    new LiteralNode(true, typeof(bool)),
                    new BodyNode(
                    [
                        new CallNode(
                            new MemberNode("print"),
                            [
                                new LiteralNode(2, typeof(int))
                            ]),
                        new CallNode(
                            new MemberNode("print"),
                            [
                                new LiteralNode(3, typeof(int))
                            ])
                    ])),
                []
                , null);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(16, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    #region Symbol tests

    [Fact]
    public void EmptyBodySymbol()
    {
        const string code = "";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseBodyLevelExpression(out var expression, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Single(symbolTable);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Empty(symbol.Children);
        Assert.Single(symbol.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[0], symbol.Tokens[0]);
    }

    [Fact]
    public void NotEmptySingleLineBody()
    {
        const string code = "13 - 3";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseBodyLevelExpression(out var expression, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(3, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Equal(2, symbol.Children.Count);
        Assert.Single(symbol.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[2], symbol.Tokens[0]);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    [Fact]
    public void NotEmptyMultipleLineBody()
    {
        const string code = """
                            if(t) print(x)
                            else
                                print(!t)
                                return 132
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseBodyLevelExpression(out var expression, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(14, symbolTable.Count);
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