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
        yield return ["a + b;", new AddNode(new MemberNode("a"), new MemberNode("b"))];
        yield return ["if(a);", new ConditionNode(new MemberNode("a"), new BodyNode([]), null)];
        yield return ["while(true);", new WhileNode(new LiteralNode(true, typeof(bool)), new BodyNode([]))];
        yield return ["break;", new BreakNode()];
        yield return ["continue;", new ContinueNode()];
        yield return ["return false;", new ReturnNode(new LiteralNode(false, typeof(bool)))];
    }
    
    [Theory]
    [MemberData(nameof(ParseBodyLevelStatement_Correct_DataProvider))]
    public void ParseBodyLevelStatement_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseStatement(context, out var statement);    
        result.ShouldBe(true);
        statement.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object?[]> ParseBodyLevelStatement_Incorrect_DataProvider()
    {
        yield return ["return", null, false];
        yield return ["break", new BreakNode(), true];
        yield return ["continue", new ContinueNode(), true];
        yield return ["while ++", null, false];
        yield return ["if )", null, false];
        yield return ["+", null, false];
    }
    
    //Condition parsing does not generate errors themself.
    [Theory]
    [MemberData(nameof(ParseBodyLevelStatement_Incorrect_DataProvider))]
    public void ParseBodyLevelStatement_Incorrect(string code, NodeBase? ast, bool resultShould)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseStatement(context, out var statement);    
        result.ShouldBe(resultShould);
        statement.ShouldBeEquivalentTo(ast);
    }
}