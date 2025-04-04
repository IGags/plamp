using Microsoft.VisualStudio.TestPlatform.Utilities;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.NodeComparers;
using plamp.Native.Parsing;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.PrecedenceExpression;

public class ConstructorDeclarationTests
{
    private static readonly RecursiveComparer Comparer = new();
    
    [Fact]
    public void ValidConstructorDeclaration()
    {
        const string code = "new int()";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConstructorNode(
                new TypeNode(
                    new MemberNode("int"),
                    null),
                []);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(4, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ValidConstructorWithGenerics()
    {
        const string code = "new List<int>()";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConstructorNode(
                new TypeNode(
                    new MemberNode("List"),
                    [
                        new TypeNode(
                            new MemberNode("int"),
                            null)
                    ]),
                []);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(7, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void ValidConstructorWithArgs()
    {
        const string code = "new string(1)";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConstructorNode(
                new TypeNode(
                    new MemberNode("string"),
                    null),
                [
                    new LiteralNode(1, typeof(int))
                ]);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(5, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void CtorWithoutParens()
    {
        const string code = "new int";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    [Fact]
    public void OnlyNewKeyword()
    {
        const string code = "new";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(-1, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }

    #region Symbol table

    [Fact]
    public void ConstructorWithoutArgsSymbolTest()
    {
        const string code = "new T()";
        var sequence = code.Tokenize().Sequence;
        var parser = new PlampNativeParser(sequence);
        
        var result = parser.TryParseWithPrecedence(out var expression);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = parser.TransactionSource.SymbolDictionary;
        Assert.Equal(3, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(sequence.TokenList[0], symbol.Tokens[0]);
        Assert.Single(symbol.Children);
    }

    [Fact]
    public void ConstructorWithArgsSymbolTest()
    {
        const string code = "new List<d>(2222,1111)";
        var sequence = code.Tokenize().Sequence;
        var parser = new PlampNativeParser(sequence);
        var result = parser.TryParseWithPrecedence(out var expression);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = parser.TransactionSource.SymbolDictionary;
        Assert.Equal(7, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(sequence.TokenList[0], symbol.Tokens[0]);
        Assert.Equal(3, symbol.Children.Count);
    }

    #endregion
}