using plamp.Ast;
using plamp.Ast.Node;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.ControlFlow;
using plamp.Ast.NodeComparers;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.Keyword;

public class ParseReturnKeywordTests
{
    private static readonly RecursiveComparer Comparer = new();
    
    [Fact]
    public void ParseReturnKeyword()
    {
        const string code = """
                            return
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ReturnNode(null);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }
    
    [Fact]
    public void ParseReturnKeywordWithExpression()
    {
        const string code = """
                            return 1+1
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ReturnNode(
                new PlusNode(
                    new ConstNode(1, typeof(int)),
                    new ConstNode(1, typeof(int))));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(5, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseReturnWithTrash()
    {
        const string code = """
                            return 1 1
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ReturnNode(
                new ConstNode(1, typeof(int)));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(5, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 8), new(0, 11));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions[0]);
    }
}