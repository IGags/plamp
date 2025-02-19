using plamp.Ast.Node;
using plamp.Ast.NodeComparers;
using plamp.Native.Parsing;
using plamp.Native.Tokenization;
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
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould 
            = new VariableDefinitionNode(
                new TypeNode(
                    new MemberNode("int"), 
                    null), 
                new MemberNode("a"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(2, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void VariableDeclarationWithKeyword()
    {
        const string code = "var a";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new VariableDefinitionNode(
                null,
                new MemberNode("a"));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(2, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void KeywordOnly()
    {
        const string code = "var";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    #region Symbol table

    [Fact]
    public void VariableWithTypeSymbol()
    {
        const string code = "int a";
        var sequence = code.Tokenize().Sequence;
        var parser = new PlampNativeParser(sequence);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);

        var table = parser.TransactionSource.SymbolDictionary;
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
        var sequence = code.Tokenize().Sequence;
        var parser = new PlampNativeParser(sequence);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);

        var table = parser.TransactionSource.SymbolDictionary;
        Assert.Equal(2, table.Count);
        Assert.Contains(expression, table);
        var symbol = table[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(sequence.TokenList[0], symbol.Tokens[0]);
        Assert.Equal(1, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, table);
        }
    }

    #endregion
}