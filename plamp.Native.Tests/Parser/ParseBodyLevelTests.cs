using plamp.Ast.Node;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.ControlFlow;
using plamp.Native.Parsing;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser;

public class ParseBodyLevelTests
{ 
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
        Assert.Equal(expressionShould, expression);
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
        Assert.Equal(expressionShould, expression);
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
        var result = parser.TryParseBodyLevelExpression(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ReturnNode(
                new MultiplyNode(
                    new PlusNode(
                        new ConstNode(2, typeof(int)),
                        new ConstNode(2, typeof(int))),
                    new ConstNode(2, typeof(int))));
        Assert.Equal(expressionShould, expression);
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
                    new ConstNode(true, typeof(bool)),
                    new BodyNode(
                    [
                        new CallNode(
                            new MemberNode("print"),
                            [
                                new ConstNode(2, typeof(int))
                            ]),
                        new CallNode(
                            new MemberNode("print"),
                            [
                                new ConstNode(3, typeof(int))
                            ])
                    ])),
                []
                , null);
        Assert.Equal(expressionShould, expression);
        Assert.Equal(16, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }
}