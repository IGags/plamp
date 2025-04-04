using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.NodeComparers;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.Keyword;

public class ParseContinueKeyword
{
    private static readonly RecursiveComparer Comparer = new();
    
    [Fact]
    public void ParseContinue()
    {
        const string code = """
                            continue
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ContinueNode();
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }
    
    [Fact]
    public void ParseContinueWithTrash()
    {
        const string code = """
                            continue 1
                            """;
        var parser = new PlampNativeParser(code);
        var transaction = parser.TransactionSource.BeginTransaction();
        var result = parser.TryParseKeywordExpression(transaction, out var expression);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ContinueNode();
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(3, parser.TokenSequence.Position);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new (0, 8), new (0, 11));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions[0]);
    }
    
    [Fact]
    public void ContinueSymbolTest()
    {
        const string code = """
                            continue
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