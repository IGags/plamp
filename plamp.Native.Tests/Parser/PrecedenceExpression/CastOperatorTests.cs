using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Extensions.Ast.Comparers;
using plamp.Native.Parsing;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.PrecedenceExpression;

public class CastOperatorTests
{
    private static readonly ExtendedRecursiveComparer Comparer = new();
    
    [Fact]
    public void ParseValidCast()
    {
        const string code = "(int)1l";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould 
            = new CastNode(
                new TypeNode(
                    new MemberNode("int"), 
                    null), 
                new LiteralNode(1L, typeof(long)));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(3, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseCastWithoutContinue()
    {
        const string code = "(int)-";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseCastWithoutCloseParen()
    {
        const string code = "(int 1";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    #region Symbol table

    [Fact]
    public void CastSymbolTest()
    {
        const string code = """
                            (string)1
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);

        var table = context.TransactionSource.SymbolDictionary;
        Assert.Equal(4, table.Count);
        Assert.Contains(expression, table);
        var symbol = table[expression];
        Assert.Empty(symbol.Tokens);
        Assert.Equal(2, symbol.Children.Count);
    }

    #endregion
}