using System;
using System.Linq;
using plamp.Ast;
using plamp.Ast.Node;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Unary;
using plamp.Ast.NodeComparers;
using plamp.Native.Parsing;
using plamp.Native.Tokenization;
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
    [InlineData("1>=2", typeof(GreaterOrEqualsNode), 1, 2)]
    [InlineData("1^1", typeof(XorNode), 1, 1)]
    [InlineData("2%5", typeof(ModuloNode), 2, 5)]
    public void ParseBinaryExpression(
        string code, Type expressionType, object left, object right)
    {
        var sequence = code.Tokenize().Sequence;
        var parser = new PlampNativeParser(sequence);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.Equal(expressionType, expression.GetType());
        var binary = (BaseBinaryNode)expression;
        var leftShould = new ConstNode(left, left.GetType());
        var rightShould = new ConstNode(right, right.GetType());
        Assert.Equal(leftShould, binary.Left, Comparer);
        Assert.Equal(rightShould, binary.Right, Comparer);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(2, parser.TokenSequence.Position);
        var dictionary = parser.TransactionSource.SymbolDictionary;
        Assert.Equal(3, dictionary.Count);
        Assert.Contains(expression, dictionary);
        var symbol = dictionary[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(sequence.TokenList[1], symbol.Tokens[0]);
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
        var sequence = code.Tokenize().Sequence;
        var parser = new PlampNativeParser(sequence);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.Equal(expressionType, expression.GetType());
        var unary = (BaseUnaryNode)expression;
        var innerShould = new ConstNode(inner, inner.GetType());
        Assert.Equal(innerShould, unary.Inner, Comparer);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(1, parser.TokenSequence.Position);
        var dictionary = parser.TransactionSource.SymbolDictionary;
        Assert.Equal(2, dictionary.Count);
        Assert.Contains(expression, dictionary);
        var symbol = dictionary[expression];
        Assert.Single(symbol.Tokens);
        Assert.Equal(prefix ? sequence.TokenList[0] : sequence.TokenList[1], symbol.Tokens[0]);
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
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(-1, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseBinaryWithoutSecond()
    {
        const string code = "1/";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould = new DivideNode(new ConstNode(1, typeof(int)), null);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(1, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseDifferentPrecedence()
    {
        const string code = "1+2*3";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new PlusNode(
                new ConstNode(1, typeof(int)),
                new MultiplyNode(
                    new ConstNode(2, typeof(int)),
                    new ConstNode(3, typeof(int))));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(4, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseParenPrecedence()
    {
        const string code = "(1+2)*3";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new MultiplyNode(
                new PlusNode(
                    new ConstNode(1, typeof(int)),
                    new ConstNode(2, typeof(int))),
                new ConstNode(3, typeof(int)));
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(6, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseEmptyParensInSubExpression()
    {
        const string code = "1*()";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new MultiplyNode(
                new ConstNode(1, typeof(int)),
                null);
        Assert.Equal(expressionShould, expression, Comparer);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(PlampNativeExceptionInfo.ExpectedExpression(),
            new(0, 2), new(0, 3));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions.First());
        Assert.Equal(3, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseEmptyParensInSubExpressionFirst()
    {
        const string code = "()*1";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Single(parser.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(PlampNativeExceptionInfo.ExpectedExpression(),
            new(0, 0), new(0, 1));
        Assert.Equal(exceptionShould, parser.TransactionSource.Exceptions.First());
        Assert.Equal(1, parser.TokenSequence.Position);
    }

    [Fact]
    public void ParseEmptyNud()
    {
        const string code = "-";
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, result);
        Assert.Null(expression);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(-1, parser.TokenSequence.Position);
    }
}