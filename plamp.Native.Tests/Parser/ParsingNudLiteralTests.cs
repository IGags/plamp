using plamp.Ast.Node;
using plamp.Native.Parsing;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser;

public class ParsingNudLiteralTests
{
	[Theory]
	[InlineData("0", 0)]
	[InlineData("1", 1)]
	//[InlineData("-1", -1)] doesn't work because parser parse unary operator first
	[InlineData("5000000000", 5000000000)]
	[InlineData("0l", 0L)]
	[InlineData("0.1", 0.1)]
	[InlineData("0s", (short)0)]
	[InlineData("0us", (ushort)0)]
	//[InlineData("-1l", -1L)] doesn't work because parser parse unary operator first
	[InlineData("1f", 1f)]
	[InlineData("3.14d", 3.14d)]
	[InlineData("true", true)]
	[InlineData("false", false)]
	[InlineData("\"abc\"", "abc")]
	[InlineData("\"\\n\"", "\n")]
	[InlineData("null", null)]
    public void ParseLiteral(string code, object value)
    {
	    var parser = new PlampNativeParser(code);
	    var result = parser.TryParseWithPrecedence(out var expression);
	    Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
	    Assert.Equal(typeof(ConstNode), expression.GetType());
	    Assert.Equal(value, ((ConstNode)expression).Value);
	    Assert.Empty(parser.TransactionSource.Exceptions);
	    Assert.Equal(0, parser.TokenSequence.Position);
    }
}