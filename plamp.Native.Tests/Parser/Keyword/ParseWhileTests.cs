using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.NodeComparers;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.Keyword;

public class ParseWhileTests
{
    private static readonly RecursiveComparer Comparer = new();
    
    [Fact]
    public void ParseWhileSingleLine()
    {
        const string code = """
                            while(true) ping()
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new WhileNode(
                new LiteralNode(true, typeof(bool)),
                new BodyNode(
                [
                    new CallNode(
                        new MemberNode("ping"),
                        [])
                ]));
        Assert.Equal(expression, expressionShould, Comparer);
        Assert.Equal(8, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseWhileMultiLine()
    {
        const string code = """
                            while(true)
                                ping()
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        transaction.Commit();
        var expressionShould
            = new WhileNode(
                new LiteralNode(true, typeof(bool)),
                new BodyNode(
                [
                    new CallNode(
                        new MemberNode("ping"),
                        [])
                ]));
        Assert.Equal(expression, expressionShould, Comparer);
        Assert.Equal(9, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseWhileMissingCloseParenSingleLine()
    {
        const string code = """
                            while(true ping()
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        transaction.Commit();
        var expressionShould
            = new WhileNode(
                new LiteralNode(true, typeof(bool)),
                new BodyNode(
                    []));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(7, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(CloseParen)),
            new(0, 5), new(0, 16),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void ParseWhileMissingCloseParenMultiLine()
    {
        const string code = """
                            while(true
                                ping()
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        transaction.Commit();
        var expressionShould
            = new WhileNode(
                new LiteralNode(true, typeof(bool)),
                new BodyNode(
                    [
                        new CallNode(
                            new MemberNode("ping"),
                            [])
                    ]));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(8, context.TokenSequence.Position);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.ParenExpressionIsNotClosed(),
            new(0, 5), new(0, 11),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void ParseWhileMissingOpenParenSingleLine()
    {
        const string code = """
                            while true) ping()
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        transaction.Commit();
        Assert.Null(expression);
        Assert.Equal(8, context.TokenSequence.Position);
        Assert.Equal(2, context.TransactionSource.Exceptions.Count);
        var exceptionShould1 = new PlampException(
            PlampNativeExceptionInfo.MissingConditionPredicate(),
            new(0, 0), new(0, 4),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould1, context.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 5), new(0, 19),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould2, context.TransactionSource.Exceptions[1]);
    }

    [Fact]
    public void ParseWhileMissingOpenParenMultiLine()
    {
        const string code = """
                            while true)
                                ping()
                            """;
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        transaction.Commit();
        Assert.Null(expression);
        Assert.Equal(9, context.TokenSequence.Position);
        Assert.Equal(3, context.TransactionSource.Exceptions.Count);
        var exceptionShould1 = new PlampException(
            PlampNativeExceptionInfo.MissingConditionPredicate(),
            new(0, 0), new(0, 4),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould1, context.TransactionSource.Exceptions[0]);
        var exceptionShould2 = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 5), new(0, 12),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould2, context.TransactionSource.Exceptions[1]);
        var exceptionShould3 = new PlampException(
            PlampNativeExceptionInfo.InvalidBody(),
            new(1, 4), new(1, 11),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould3, context.TransactionSource.Exceptions[2]);
    }

    [Fact]
    public void ParseWhileWithEmptyCondition()
    {
        const string code = """
                            while() ping()
                            """;
        
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var result = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        transaction.Commit();
        var expressionShould
            = new WhileNode(
                null,
                new BodyNode(
                [
                    new CallNode(
                        new MemberNode("ping"),
                        [])
                ]));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(7, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.EmptyConditionPredicate(),
            new(0, 5), new(0, 6),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }

    #region Symbol dictionary

    [Fact]
    public void SymbolWhileSingleLine()
    {
        const string code = """
                            while(true) i++
                            """;

        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var res = PlampNativeParser.TryParseKeywordExpression(transaction, out var expression, context);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, res);
        
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(5, symbolTable.Count);
        var first = symbolTable[expression];
        Assert.Single(first.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[0], first.Tokens[0]);
        Assert.Equal(2, first.Children.Count);
        foreach (var child in first.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    #endregion
}