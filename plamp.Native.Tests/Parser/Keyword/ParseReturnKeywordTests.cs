using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.NodeComparers;
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
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ReturnNode(null);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }
    
    [Fact]
    public void ParseReturnKeywordWithExpression()
    {
        const string code = """
                            return 1+1
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ReturnNode(
                new PlusNode(
                    new LiteralNode(1, typeof(int)),
                    new LiteralNode(1, typeof(int))));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(5, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseReturnWithTrash()
    {
        const string code = """
                            return 1 1
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ReturnNode(
                new LiteralNode(1, typeof(int)));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(5, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 8), new(0, 11));
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }

    #region Symbol dictionary

    [Fact]
    public void SymbolEmptyReturn()
    {
        const string code = """
                            return
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

    public void SymbolReturnWithValue()
    {
        const string code = """
                            return 0
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
        Assert.Single(val.Children);
        Assert.Single(val.Tokens);
        var token = val.Tokens[0];
        Assert.Equal(context.TokenSequence.TokenList[0], token);
        Assert.Contains(val.Children[0], symbolDictionary);
    }
    #endregion
}