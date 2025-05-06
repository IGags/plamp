using System;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.Ast.NodeComparers;
using plamp.Native.Parsing;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.PrecedenceExpression;

public class SimpleMathTests
{
    private static readonly RecursiveComparer Comparer = new();
    
    [Theory]
    [InlineData("1+1", typeof(PlusNode), 1, 1)]
    [InlineData("1-1", typeof(MinusNode), 1, 1)]
    [InlineData("1*1", typeof(MultiplyNode), 1, 1)]
    [InlineData("1/1", typeof(DivideNode), 1, 1)]
    [InlineData("true&&false", typeof(AndNode), true, false)]
    [InlineData("false||true", typeof(OrNode), false, true)]
    [InlineData("1&1", typeof(BitwiseAndNode), 1, 1)]
    [InlineData("1|0", typeof(BitwiseOrNode), 1, 0)]
    [InlineData("1==2", typeof(EqualNode), 1, 2)]
    [InlineData("1!=2", typeof(NotEqualNode), 1, 2)]
    [InlineData("1<2", typeof(LessNode), 1, 2)]
    [InlineData("1>2", typeof(GreaterNode), 1, 2)]
    [InlineData("1<=2", typeof(LessOrEqualNode), 1, 2)]
    [InlineData("1>=2", typeof(GreaterOrEqualNode), 1, 2)]
    [InlineData("1^1", typeof(XorNode), 1, 1)]
    [InlineData("2%5", typeof(ModuloNode), 2, 5)]
    public void ParseBinaryExpression(
        string code, Type expressionType, object left, object right)
    {
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.Equal(expressionType, expression.GetType());
        var binary = (BaseBinaryNode)expression;
        var leftShould = new LiteralNode(left, left.GetType());
        var rightShould = new LiteralNode(right, right.GetType());
        Assert.Equal(leftShould, binary.Left, Comparer);
        Assert.Equal(rightShould, binary.Right, Comparer);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(2, context.TokenSequence.Position);
        var dictionary = context.TransactionSource.SymbolDictionary;
        Assert.Equal(3, dictionary.Count);
        Assert.Contains(expression, dictionary);
        var symbol = dictionary[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(context.TokenSequence.TokenList[1], symbol.Tokens[0]);
        Assert.Equal(2, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, dictionary);
        }
    }

    [Theory]
    [InlineData("-1", typeof(UnaryMinusNode), 1, true)]
    [InlineData("!false", typeof(NotNode), false, true)]
    [InlineData("++1", typeof(PrefixIncrementNode), 1, true)]
    [InlineData("--1", typeof(PrefixDecrementNode), 1, true)]
    [InlineData("1++", typeof(PostfixIncrementNode), 1, false)]
    [InlineData("1--", typeof(PostfixDecrementNode), 1, false)]
    public void ParseUnaryExpression(string code, Type expressionType, object inner, bool prefix)
    {
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.Equal(expressionType, expression.GetType());
        var unary = (BaseUnaryNode)expression;
        var innerShould = new LiteralNode(inner, inner.GetType());
        Assert.Equal(innerShould, unary.Inner, Comparer);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(1, context.TokenSequence.Position);
        var dictionary = context.TransactionSource.SymbolDictionary;
        Assert.Equal(2, dictionary.Count);
        Assert.Contains(expression, dictionary);
        var symbol = dictionary[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(prefix ? context.TokenSequence.TokenList[0] : context.TokenSequence.TokenList[1], symbol.Tokens[0]);
        Assert.Equal(1, symbol.Children.Count);
        foreach (var child in symbol.Children)
        {
            Assert.Contains(child, dictionary);
        }
    }

    [Fact]
    public void ParseBinaryWithoutFirst()
    {
        const string code = "/1";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(-1, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseBinaryWithoutSecond()
    {
        const string code = "1/";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould = new DivideNode(new LiteralNode(1, typeof(int)), null);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(1, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseDifferentPrecedence()
    {
        const string code = "1+2*3";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new PlusNode(
                new LiteralNode(1, typeof(int)),
                new MultiplyNode(
                    new LiteralNode(2, typeof(int)),
                    new LiteralNode(3, typeof(int))));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(4, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseParenPrecedence()
    {
        const string code = "(1+2)*3";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new MultiplyNode(
                new PlusNode(
                    new LiteralNode(1, typeof(int)),
                    new LiteralNode(2, typeof(int))),
                new LiteralNode(3, typeof(int)));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(6, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseEmptyParensInSubExpression()
    {
        const string code = "1*()";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new MultiplyNode(
                new LiteralNode(1, typeof(int)),
                null);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(PlampNativeExceptionInfo.ExpectedExpression(),
            new(0, 2), new(0, 3),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions.First());
        Assert.Equal(3, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseEmptyParensInSubExpressionFirst()
    {
        const string code = "()*1";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(PlampNativeExceptionInfo.ExpectedExpression(),
            new(0, 0), new(0, 1),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions.First());
        Assert.Equal(1, context.TokenSequence.Position);
    }

    [Fact]
    public void ParseEmptyNud()
    {
        const string code = "-";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(-1, context.TokenSequence.Position);
    }
}