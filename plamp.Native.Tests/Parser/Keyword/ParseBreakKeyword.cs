using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.NodeComparers;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.Keyword;

public class ParseBreakKeyword
{
    private static readonly RecursiveComparer Comparer = new();
    
    [Fact]
    public void ParseBreak()
    {
        const string code = """
                            break
                            """;
        
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new BreakNode();
        Assert.Equal(expression, expressionShould, Comparer);
        Assert.Equal(1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseBreakWithContinuation()
    {
        const string code = """
                            break 1
                            """;
        
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new BreakNode();
        Assert.Equal(expression, expressionShould, Comparer);
        Assert.Equal(3, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 5), new(0, 8),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void BreakSymbolTest()
    {
        const string code = """
                            break
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolDictionary = context.TransactionSource.SymbolDictionary;
        Assert.Single(symbolDictionary);
        Assert.Contains(expression, symbolDictionary);
        var val = symbolDictionary[expression];
        Assert.Empty(val.Children);
        Assert.Single(val.Tokens);
        var token = val.Tokens[0];
        Assert.Equal(context.TokenSequence.TokenList[0], token);
    }
}