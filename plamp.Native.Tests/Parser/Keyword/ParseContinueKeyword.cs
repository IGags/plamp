using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Extensions.Ast.Comparers;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.Keyword;

public class ParseContinueKeyword
{
    private static readonly ExtendedRecursiveComparer Comparer = new();
    
    [Fact]
    public void ParseContinue()
    {
        const string code = """
                            continue
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ContinueNode();
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }
    
    [Fact]
    public void ParseContinueWithTrash()
    {
        const string code = """
                            continue 1
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ContinueNode();
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(3, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new (0, 8), new (0, 11),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }
    
    [Fact]
    public void ContinueSymbolTest()
    {
        const string code = """
                            continue
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