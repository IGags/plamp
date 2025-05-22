using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Extensions.Ast.Comparers;
using plamp.Native.Parsing;
using Xunit;

#pragma warning disable CS0618
namespace plamp.Native.Tests.Parser.PrecedenceExpression;

//Here live corner cases or just complex expressions
//Test collection will be expanded
public class SingleLineExpressionParsingComplexCasesTests
{
    [Fact]
    public void DeclareAssignCastAndSub()
    {
        const string code = "int i = (int)(1.2/3 + getRandNum())";
        var context = ParserTestHelper.GetContext(code);
        var result = PlampNativeParser.TryParseWithPrecedence(out var expression, context);
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, result);
        var expressionShould
            = new AssignNode(
                new VariableDefinitionNode(
                    new TypeNode(
                        new MemberNode("int"),
                        null),
                    new MemberNode("i")),
                new CastNode(
                    new TypeNode(
                        new MemberNode("int"),
                        null),
                    new PlusNode(
                        new DivideNode(
                            new LiteralNode(1.2, typeof(double)),
                            new LiteralNode(3, typeof(int))),
                        new CallNode(
                            null,
                            new MemberNode("getRandNum"),
                            []))));
        Assert.Equal(expressionShould, expression, new ExtendedRecursiveComparer());
        Assert.Equal(19, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }
}