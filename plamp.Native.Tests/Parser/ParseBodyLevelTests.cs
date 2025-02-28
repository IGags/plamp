using Microsoft.VisualStudio.TestPlatform.Utilities;
using plamp.Ast.Node;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.ControlFlow;
using plamp.Ast.NodeComparers;
using plamp.Native.Parsing;
using plamp.Native.Tokenization;
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
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseBodyLevelExpression(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new MinusNode(
                new PlusNode(
                    new MemberNode("a"),
                    new MemberNode("b")),
                new MemberNode("c"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(4, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseValidSingleLineWithTrashAfter()
    {
        const string code = """
                            a+b-c 1
                            """;
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseBodyLevelExpression(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new MinusNode(
                new PlusNode(
                    new MemberNode("a"),
                    new MemberNode("b")),
                new MemberNode("c"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(4, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    //But keyword expression advance to end of line
    [Fact]
    public void ParseSmthKeywordSingleLine()
    {
        const string code = """
                            return 2 + 2 ->
                                    * 2
                            """;
        var parser = new PlampNativeParser(code);
        //HoW does dis works? watch commit
        var result = parser.TryParseBodyLevelExpression(out var expression);
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
        Assert.Equal(15, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseSmthKeywordMultiLine()
    {
        const string code = """
                            if(true)
                                print(2)
                                print(3)
                            """;
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseBodyLevelExpression(out var expression);
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
        Assert.Equal(16, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    #region Symbol tests

    [Fact]
    public void EmptyBodySymbol()
    {
        const string code = "";
        var tokenSequence = code.Tokenize().Sequence;
        var parser = new PlampNativeParser(tokenSequence);
        var result = parser.TryParseBodyLevelExpression(out var expression);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = parser.TransactionSource.SymbolDictionary;
        Assert.Single(symbolTable);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Empty(symbol.Children);
        Assert.Single(symbol.Tokens);
        Assert.Equal(tokenSequence.TokenList[0], symbol.Tokens[0]);
    }

    [Fact]
    public void NotEmptySingleLineBody()
    {
        const string code = "13 - 3";
        var tokenSequence = code.Tokenize().Sequence;
        var parser = new PlampNativeParser(tokenSequence);
        var result = parser.TryParseBodyLevelExpression(out var expression);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = parser.TransactionSource.SymbolDictionary;
        Assert.Equal(3, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Equal(2, symbol.Children.Count);
        Assert.Single(symbol.Tokens);
        Assert.Equal(tokenSequence.TokenList[2], symbol.Tokens[0]);
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
        var tokenSequence = code.Tokenize().Sequence;
        var parser = new PlampNativeParser(tokenSequence);
        var result = parser.TryParseBodyLevelExpression(out var expression);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = parser.TransactionSource.SymbolDictionary;
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