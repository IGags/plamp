using plamp.Ast.Node;
using plamp.Ast.Node.Binary;
using plamp.Native.Parsing;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.PrecedenceExpression;

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

    [Fact]
    public void ParseMemberAccessSequence()
    {
        const string code = "hey.dude.you.cool";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new MemberAccessNode(
                new MemberAccessNode(
                    new MemberAccessNode(
                        new MemberNode("hey"),
                        new MemberNode("dude")),
                    new MemberNode("you")),
                new MemberNode("cool"));
        Assert.Equal(expressionShould, expression);
        Assert.Equal(6, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }
    
    [Fact]
    public void ParseAccessWithoutNextMember()
    {
        const string code = "hey.";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.Equal(new MemberNode("hey"), expression);
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseCallExpression()
    {
        const string code = "greet()";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new CallNode(
                new MemberNode("greet"),
                []);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(2, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseInvalidCallExpression()
    {
        const string code = "greet.()";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould = new MemberNode("greet");
        Assert.Equal(expressionShould, expression);
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseCallWithArgs()
    {
        const string code = "greet(\"hi\"+\"you\", 1)";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new CallNode(
                new MemberNode("greet"),
                [
                    new PlusNode(
                        new ConstNode("hi", typeof(string)),
                        new ConstNode("you", typeof(string))),
                    new ConstNode(1, typeof(int))
                ]);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(8, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseCallSequence()
    {
        const string code = "arg.greet(greeter).bye()";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
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
        Assert.Equal(expressionShould, expression);
        Assert.Equal(9, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseIndexer()
    {
        const string code = "arr[0]";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new IndexerNode(
                new MemberNode("arr"),
                [
                    new ConstNode(0, typeof(int))
                ]);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(3, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseMultidimensionalIndexer()
    {
        const string code = "arr[0,1]";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new IndexerNode(
                new MemberNode("arr"),
                [
                    new ConstNode(0, typeof(int)),
                    new ConstNode(1, typeof(int))
                ]);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(5, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseIndexerSequence()
    {
        const string code = "arr[0][\"hi\"]";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new IndexerNode(
                new IndexerNode(
                    new MemberNode("arr"),
                    [
                        new ConstNode(0, typeof(int))
                    ]),
                [
                    new ConstNode("hi", typeof(string))
                ]);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(6, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }
}