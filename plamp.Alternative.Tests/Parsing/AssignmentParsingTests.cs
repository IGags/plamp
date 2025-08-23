using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class AssignmentParsingTests
{
    public static IEnumerable<object[]> ParseAssignment_Correct_DataProvider()
    {
        yield return 
        [
            "int a := 5",
            new AssignNode(
                new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a")),
                new LiteralNode(5, typeof(int))
            )
        ];
        yield return
        [
            "a := 14.3",
            new AssignNode(
                new MemberNode("a"),
                new LiteralNode(14.3, typeof(double)))
        ];
    }
    
    [Theory]
    [MemberData(nameof(ParseAssignment_Correct_DataProvider))]
    public void ParseAssignment_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseAssignment(context, out var assign);
        context.Exceptions.ShouldBeEmpty();
        parsed.ShouldBe(true);
        assign.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object[]> ParseAssignment_Incorrect_DataProvider()
    {
        yield return ["+", new List<string>()];
        yield return ["fn", new List<string>()];

        yield return ["int a", new List<string>{PlampExceptionInfo.ExpectedAssignment().Code}];
        yield return ["int a := ", new List<string>{PlampExceptionInfo.ExpectedAssignmentSource().Code}];
        yield return ["int a := use", new List<string>{PlampExceptionInfo.ExpectedAssignmentSource().Code}];
        
        yield return ["a", new List<string>{PlampExceptionInfo.ExpectedAssignment().Code}];
        yield return ["a := ", new List<string>{PlampExceptionInfo.ExpectedAssignmentSource().Code}];
        yield return ["a := use", new List<string>{PlampExceptionInfo.ExpectedAssignmentSource().Code}];
    }
    
    [Theory]
    [MemberData(nameof(ParseAssignment_Incorrect_DataProvider))]
    public void ParseAssignment_Incorrect(string code, List<string> errorCodes)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseAssignment(context, out _);
        parsed.ShouldBe(false);
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodes);
    }
}