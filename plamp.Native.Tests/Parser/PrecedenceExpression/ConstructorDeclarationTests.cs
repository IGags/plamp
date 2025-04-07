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
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new ConstructorNode(
                new TypeNode(
                    new MemberNode("int"),
                    null),
                []);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Equal(4, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ValidConstructorWithGenerics()
    {
        const string code = "new List<int>()";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
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
        Assert.Equal(7, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void ValidConstructorWithArgs()
    {
        const string code = "new string(1)";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
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
        Assert.Equal(5, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void CtorWithoutParens()
    {
        const string code = "new int";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void OnlyNewKeyword()
    {
        const string code = "new";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    #region Symbol table

    [Fact]
    public void ConstructorWithoutArgsSymbolTest()
    {
        const string code = "new T()";
        var context = ParserTestHelper.GetContext(code);
        
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(3, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[0], symbol.Tokens[0]);
        Assert.Single(symbol.Children);
    }

    [Fact]
    public void ConstructorWithArgsSymbolTest()
    {
        const string code = "new List<d>(2222,1111)";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var symbolTable = context.TransactionSource.SymbolDictionary;
        Assert.Equal(7, symbolTable.Count);
        Assert.Contains(expression, symbolTable);
        var symbol = symbolTable[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[0], symbol.Tokens[0]);
        Assert.Equal(3, symbol.Children.Count);
    }

    #endregion
}