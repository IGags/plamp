using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.NodeComparers;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser;

public class ParseUsingTests
{
    private static readonly RecursiveComparer Comparer = new();
    
    [Fact]
    public void ParseValidUsingSingleLine()
    {
        const string code = """
                            use std
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new UseNode(
                new MemberNode("std"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(3, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseValidUsingWordChain()
    {
        const string code = """
                            use std.collection
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new UseNode(
                new MemberAccessNode(new MemberNode("std"), new MemberNode("collection")));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(5, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ParseUnknownCharBetweenDefinition()
    {
        const string code = """
                            use + std
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(5, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.InvalidUsingName(),
            new(0, 4), new(0, 10),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void ParseUnknownCharAfterDefinition()
    {
        const string code = """
                            use std =
                            """;
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new UseNode(
                new MemberNode("std"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(5, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(EndOfLine)),
            new(0, 7), new(0, 10),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void ParseEmptyUse()
    {
        const string code = "use ";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.InvalidUsingName(), 
            new(0, 4), new(0, 5),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }
    
    #region Symbol table

    [Fact]
    public void UseSymbol()
    {
        const string code = "use std";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseTopLevel(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);

        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(2, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[0], symbol.Tokens[0]);
        Assert.Single(symbol.Children);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, symbolTable);
        }
    }

    #endregion
}