using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.NodeComparers;
using plamp.Native.Parsing;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.PrecedenceExpression;

public class VariableDeclarationTests
{
    private static readonly RecursiveComparer Comparer = new();
    
    [Fact]
    public void VariableDeclarationWithType()
    {
        const string code = "int a";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould 
            = new VariableDefinitionNode(
                new TypeNode(
                    new MemberNode("int"), 
                    null), 
                new MemberNode("a"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(2, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void VariableDeclarationWithKeyword()
    {
        const string code = "var a";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new VariableDefinitionNode(
                null,
                new MemberNode("a"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(2, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void KeywordOnly()
    {
        const string code = "var";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    #region Symbol table

    [Fact]
    public void VariableWithTypeSymbol()
    {
        const string code = "int a";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);

        var table = context.TransactionSource.SymbolDictionary;
        Assert.Equal(4, table.Count);
        Assert.Contains(expression, table);
        var symbol = table[expression];
        Assert.Empty(symbol.Tokens);
        Assert.Equal(2, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, table);
        }
    }

    [Fact]
    public void VariableWithKeywordSymbol()
    {
        const string code = "var a";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);

        var table = context.TransactionSource.SymbolDictionary;
        Assert.Equal(2, table.Count);
        Assert.Contains(expression, table);
        var symbol = table[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[0], symbol.Tokens[0]);
        Assert.Equal(1, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, table);
        }
    }

    #endregion
}