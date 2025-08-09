using System.Collections.Generic;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class ConditionParsingTests
{
    public static IEnumerable<object[]> ParseCondition_Correct_DataProvider()
    {
        yield return ["if(true);", new ConditionNode(new LiteralNode(true, typeof(bool)), new BodyNode([]), null)];
        yield return ["if(true) {}", new ConditionNode(new LiteralNode(true, typeof(bool)), new BodyNode([]), null)];
        yield return 
        [
            "if(true) fn1();", 
            new ConditionNode(
                new LiteralNode(true, typeof(bool)), 
                new BodyNode([
                    new CallNode(null, new MemberNode("fn1"), [])
                ]), 
                null)
        ];
        yield return
        [
            """
            if(a) {
                a := b;
            }
            """,
            new ConditionNode(
                new MemberNode("a"),
                new BodyNode([
                    new AssignNode(new MemberNode("a"), new MemberNode("b"))
                ]),
                null)
        ];
        yield return ["if(true); else;", new ConditionNode(new LiteralNode(true, typeof(bool)), new BodyNode([]), new BodyNode([]))];
        yield return
        [
            """
            if(true){
            } else {
                print();
            }
            """,
            new ConditionNode(
                new LiteralNode(true, typeof(bool)),
                new BodyNode([]),
                new BodyNode([
                    new CallNode(null, new MemberNode("print"), [])
                ]))
        ];
    }
    
    [Theory]
    [MemberData(nameof(ParseCondition_Correct_DataProvider))]
    public void ParseCondition_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseCondition(context, out var condition);    
        result.ShouldBe(true);
        condition.ShouldBeEquivalentTo(ast);
    }

    //Condition parsing does not generate errors themself.
    [Theory]
    [InlineData("if false)")]
    [InlineData("")]
    [InlineData("+")]
    [InlineData("if true")]
    [InlineData("if(")]
    public void ParseCondition_Incorrect(string code)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseCondition(context, out var condition);    
        result.ShouldBe(false);
        condition.ShouldBeNull();
    }
}