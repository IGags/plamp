using System.Collections.Generic;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class ParseBodyLevelStatement
{
    public static IEnumerable<object[]> ParseBodyLevelStatement_Correct_DataProvider()
    {
        yield return ["a + b;", new List<NodeBase>{new AddNode(new MemberNode("a"), new MemberNode("b"))}];
        yield return ["if(a);", new List<NodeBase>{new ConditionNode(new MemberNode("a"), new BodyNode([]), null)}];
        yield return ["while(true);", new List<NodeBase>{new WhileNode(new LiteralNode(true, typeof(bool)), new BodyNode([]))}];
        yield return ["break;", new List<NodeBase>{new BreakNode()}];
        yield return ["continue;", new List<NodeBase>{new ContinueNode()}];
        yield return ["return false;", new List<NodeBase>{new ReturnNode(new LiteralNode(false, typeof(bool)))}];
    }
    
    [Theory]
    [MemberData(nameof(ParseBodyLevelStatement_Correct_DataProvider))]
    public void ParseBodyLevelStatement_Correct(string code, List<NodeBase> ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseStatement(context, out var statement);    
        context.Exceptions.ShouldBeEmpty();
        result.ShouldBe(true);
        statement.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object?[]> ParseBodyLevelStatement_Incorrect_DataProvider()
    {
        yield return ["return", null, false];
        yield return ["break", new List<NodeBase>{new BreakNode()}, true];
        yield return ["continue", new List<NodeBase>{new ContinueNode()}, true];
        yield return ["while ++", null, false];
        yield return ["if )", null, false];
        yield return ["+", null, false];
    }
    
    //Condition parsing does not generate errors themself.
    [Theory]
    [MemberData(nameof(ParseBodyLevelStatement_Incorrect_DataProvider))]
    public void ParseBodyLevelStatement_Incorrect(string code, object? ast, bool resultShould)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseStatement(context, out var statement);    
        result.ShouldBe(resultShould);
        statement.ShouldBeEquivalentTo(ast);
    }
}