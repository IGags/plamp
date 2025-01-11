using plamp.Ast.Node;
using plamp.Native.Parsing;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser;

public class ExpressionPostfixTests
{
    [Fact]
    public void ParseSingleMember()
    {
        const string code = "hi";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould = new MemberNode("hi");
        Assert.Equal(expressionShould, expression);
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseMemberAccess()
    {
        const string code = "hey.dude";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new MemberAccessNode(
                new MemberNode("hey"), 
                new MemberNode("dude"));
        Assert.Equal(expressionShould, expression);
        Assert.Equal(2, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }
}