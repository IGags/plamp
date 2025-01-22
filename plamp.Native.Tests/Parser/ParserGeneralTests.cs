using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Native.Parsing;
using Xunit;

namespace plamp.Native.Tests.Parser;

public class ParserGeneralTests
{
    [Fact]
    public void ParseFindMaximum()
    {
        const string code = """
                            def int max(List<int> list)
                                var max = int.Min()
                                for(var item in list)
                                    if(max < item)
                                        max = item
                                return max
                            """;
        var parser = new PlampNativeParser();
        var result = parser.Parse(code);
        Assert.Empty(result.Exceptions);
        Assert.Single(result.NodeList);
        var expressionShould
            = new DefNode(
                new TypeNode(
                    new MemberNode("int"),
                    null),
                new MemberNode("max"),
                [
                    new ParameterNode(
                        new TypeNode(
                            new MemberNode("List"),
                            [
                                new TypeNode(
                                    new MemberNode("int"),
                                    null)
                            ]),
                        new MemberNode("list"))
                ],
                new BodyNode(
                [
                    new AssignNode(
                        new VariableDefinitionNode(
                            null,
                            new MemberNode("max")),
                        new CallNode(
                            new MemberAccessNode(
                                new MemberNode("int"),
                                new MemberNode("Min")),
                            [])),
                    new ForeachNode(
                        new VariableDefinitionNode(
                            null,
                            new MemberNode("item")),
                        new MemberNode("list"),
                        new BodyNode(
                        [
                            new ConditionNode(
                                new ClauseNode(
                                    new LessNode(
                                        new MemberNode("max"),
                                        new MemberNode("item")),
                                    new BodyNode(
                                    [
                                        new AssignNode(
                                            new MemberNode("max"),
                                            new MemberNode("item"))
                                    ])),
                                [],
                                null)
                        ]))
                ]));
        Assert.Equal(expressionShould, result.NodeList[0]);
    }
}