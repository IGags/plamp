using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.NodeComparers;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser;

public class ParseUsingTests
{
    private static readonly RecursiveComparer Comparer = new();
    
    [Fact]
    public void ParseValidUsingSingleLine()
    {
        const string code = """
                            use std
                            """;
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseTopLevel(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new UseNode(
                new MemberNode("std"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(3, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseValidUsingWordChain()
    {
        const string code = """
                            use std.collection
                            """;
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseTopLevel(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new UseNode(
                new MemberAccessNode(new MemberNode("std"), new MemberNode("collection")));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(5, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseUnknownCharBetweenDefinition()
    {
        const string code = """
                            use + std
                            """;
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseTopLevel(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(5, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.InvalidUsingName(),
            new(0, 4), new(0, 10));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void ParseUnknownCharAfterDefinition()
    {
        const string code = """
                            use std =
                            """;
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseTopLevel(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new UseNode(
                new MemberNode("std"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(5, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 7), new(0, 10));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void ParseEmptyUse()
    {
        const string code = "use ";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseTopLevel(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(PlampNativeExceptionInfo.InvalidUsingName(), new(0, 4), new(0, 5));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions[0]);
    }
    
    #region Symbol table

    [Fact]
    public void UseSymbol()
    {
        const string code = "use std";
        var tokenSequence = code.Tokenize().Sequence;
        var parser = new PlampNativeParser(tokenSequence);
        var result = parser.TryParseTopLevel(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);

        var symbolTable = parser.TransactionSource.SymbolDictionary;
        Assert.Equal(2, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(tokenSequence.TokenList[0], symbol.Tokens[0]);
        Assert.Single(symbol.Children);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    #endregion
}