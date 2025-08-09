using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class BodyParsingTests
{
    public static IEnumerable<object[]> ParseBody_Correct_DataProvider()
    {
        yield return ["{}", new BodyNode([])];
        yield return [";", new BodyNode([])];
        yield return ["return;", new BodyNode([new ReturnNode(null)])];
        yield return ["{a; b;}", new BodyNode([new MemberNode("a"), new MemberNode("b")])];
        yield return
        [
            """
            {
                a := b;
                b := a;
            }
            """,
            new BodyNode([
                new AssignNode(new MemberNode("a"), new MemberNode("b")),
                new AssignNode(new MemberNode("b"), new MemberNode("a"))
            ])
        ];
    }
    
    [Theory]
    [MemberData(nameof(ParseBody_Correct_DataProvider))]
    public void ParseBody_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseBody(context, out var body);
        result.ShouldBe(true);
        body.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object[]> ParseBody_Incorrect_DataProvider()
    {
        yield return
        [
            "{ aaa; ", new BodyNode([new MemberNode("aaa")]),
            new List<string> { PlampExceptionInfo.ExpectedClosingCurlyBracket().Code }
        ];
    }
    
    //Body parsing always true.
    //Check only body parsing generated errors, not underlying methods.
    [Theory]
    [MemberData(nameof(ParseBody_Incorrect_DataProvider))]
    public void ParseBody_Incorrect(string code, NodeBase ast, List<string> errorCodes)
    {
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseBody(context, out var body);
        result.ShouldBe(true);
        body.ShouldBeEquivalentTo(ast);
        foreach (var errorCode in errorCodes)
        {
            context.Exceptions.Select(x => x.Code).ShouldContain(errorCode);
        }
    }
}