using plamp.Ast;
using plamp.Ast.Node;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser;

public class ParseUsingTests
{
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
        Assert.Equal(expressionShould, expression);
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
        Assert.Equal(expressionShould, expression);
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
        Assert.Equal(expressionShould, expression);
        Assert.Equal(5, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 7), new(0, 10));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions[0]);
    }
}