using System.Collections.Generic;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class WhileLoopParsingTests
{
    public static IEnumerable<object[]> ParseWhileLoop_Correct_DataProvider()
    {
        yield return ["while(true);", new WhileNode(new LiteralNode(true, typeof(bool)), new BodyNode([]))];
        yield return
        [
            "while(!a) fn1();",
            new WhileNode(new NotNode(new MemberNode("a")),
                new BodyNode([new CallNode(null, new MemberNode("fn1"), [])]))
        ];
        yield return
        [
            "while(fn2()) {}",
            new WhileNode(new CallNode(null, new MemberNode("fn2"), []), new BodyNode([]))
        ];
        yield return
        [
            """
            while(false) {
                calli(1245*244);
            }
            """,
            new WhileNode(
                new LiteralNode(false, typeof(bool)),
                new BodyNode([
                    new CallNode(null, new MemberNode("calli"), 
                        [new MulNode(new LiteralNode(1245, typeof(int)), new LiteralNode(244, typeof(int)))])
                ])
            )
        ];
    } 
    
    [Theory]
    [MemberData(nameof(ParseWhileLoop_Correct_DataProvider))]
    public void ParseWhileLoop_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseWhileLoop(context, out var loop);
        context.Exceptions.ShouldBeEmpty();
        parsed.ShouldBe(true);
        loop.ShouldBeEquivalentTo(ast);
    }
    
    //While does not generate errors themselves.
    [Theory]
    [InlineData("+")]
    [InlineData("while")]
    [InlineData("while(")]
    [InlineData("while true)")]
    public void ParseWhileLoop_Incorrect(string code)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseWhileLoop(context, out var loop);
        parsed.ShouldBe(false);
        loop.ShouldBeNull();
    }
}