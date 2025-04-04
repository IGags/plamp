using Microsoft.VisualStudio.TestPlatform.Utilities;
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
        var parser = new PlampNativeParser();
        var context = new ParsingContext(code.Tokenize())
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new BreakNode();
        Assert.Equal(expression, expressionShould, Comparer);
        Assert.Equal(1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseBreakWithContinuation()
    {
        const string code = """
                            break 1
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new BreakNode();
        Assert.Equal(expression, expressionShould, Comparer);
        Assert.Equal(3, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 5), new(0, 8));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void BreakSymbolTest()
    {
        const string code = """
                            break
                            """;
        var tokenRes = code.Tokenize();
        var parser = new PlampNativeParser(tokenRes.Sequence);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolDictionary = parser.TransactionSource.SymbolDictionary;
        Assert.Single(symbolDictionary);
        Assert.Contains(expression, symbolDictionary);
        var val = symbolDictionary[expression];
        Assert.Empty(val.Children);
        Assert.Single(val.Tokens);
        var token = val.Tokens[0];
        Assert.Equal(tokenRes.Sequence.TokenList[0], token);
    }
}