using plamp.Ast.Node;
using plamp.Ast.NodeComparers;
using plamp.Native.Parsing;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.PrecedenceExpression;

public class ConstructorDeclarationTests
{
    private static readonly RecursiveComparer Comparer = new();
    
    [Fact]
    public void ValidConstructorDeclaration()
    {
        const string code = "new int()";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConstructorNode(
                new TypeNode(
                    new MemberNode("int"),
                    null),
                []);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(4, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ValidConstructorWithGenerics()
    {
        const string code = "new List<int>()";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConstructorNode(
                new TypeNode(
                    new MemberNode("List"),
                    [
                        new TypeNode(
                            new MemberNode("int"),
                            null)
                    ]),
                []);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(7, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ValidConstructorWithArgs()
    {
        const string code = "new string(1)";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConstructorNode(
                new TypeNode(
                    new MemberNode("string"),
                    null),
                [
                    new ConstNode(1, typeof(int))
                ]);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(5, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void CtorWithoutParens()
    {
        const string code = "new int";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void OnlyNewKeyword()
    {
        const string code = "new";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }
}