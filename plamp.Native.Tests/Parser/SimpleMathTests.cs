using System;
using System.Runtime.InteropServices;
using plamp.Ast.Node;
using plamp.Ast.Node.Binary;
using plamp.Native.Parsing;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser;

public class SimpleMathTests
{
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
    public void ParseBinaryExpression(
        string code, Type expressionType, object left, object right)
    {
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        Assert.Equal(expressionType, expression.GetType());
        var binary = (BaseBinaryNode)expression;
        var leftShould = new ConstNode(left, left.GetType());
        var rightShould = new ConstNode(right, right.GetType());
        Assert.Equal(leftShould, binary.Left);
        Assert.Equal(rightShould, binary.Right);
        Assert.Empty(parser.TransactionSource.Exceptions);
        Assert.Equal(2, parser.TokenSequence.Position);
    }
}