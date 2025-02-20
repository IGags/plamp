using plamp.Ast.Node;
using plamp.Ast.NodeComparers;
using plamp.Native.Parsing;
using plamp.Native.Tokenization;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.PrecedenceExpression;

public class CastOperatorTests
{
    private static readonly RecursiveComparer Comparer = new();
    
    [Fact]
    public void ParseValidCast()
    {
        const string code = "(int)1l";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould 
            = new CastNode(
                new TypeNode(
                    new MemberNode("int"), 
                    null), 
                new ConstNode(1L, typeof(long)));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(3, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseCastWithoutContinue()
    {
        const string code = "(int)-";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseCastWithoutCloseParen()
    {
        const string code = "(int 1";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    #region Symbol table

    [Fact]
    public void CastSymbolTest()
    {
        const string code = """
                            (string)1
                            """;
        var tokenSequence = code.Tokenize().Sequence;
        var parser = new PlampNativeParser(tokenSequence);
        var result = parser.TryParseWithPrecedence(out var expression);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);

        var table = parser.TransactionSource.SymbolDictionary;
        Assert.Equal(4, table.Count);
        Assert.Contains(expression, table);
        var symbol = table[expression];
        Assert.Empty(symbol.Tokens);
        Assert.Equal(2, symbol.Children.Count);
    }

    #endregion
}