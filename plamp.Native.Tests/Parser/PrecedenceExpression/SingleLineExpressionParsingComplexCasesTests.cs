using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.NodeComparers;
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
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseWithPrecedence(out var expression);
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
                            new ConstNode(1.2, typeof(double)),
                            new ConstNode(3, typeof(int))),
                        new CallNode(
                            new MemberNode("getRandNum"),
                            []))));
        Assert.Equal(expressionShould, expression, new RecursiveComparer());
        Assert.Equal(19, parser.TokenSequence.Position);
        Assert.Empty(parser.TransactionSource.Exceptions);
    }
}