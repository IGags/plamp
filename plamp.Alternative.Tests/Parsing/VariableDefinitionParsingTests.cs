using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class VariableDefinitionParsingTests
{
    [Fact]
    public void ParseVariableDefinition_Correct()
    {
        const string code = "int a";
        var ast = new VariableDefinitionNode(new TypeNode(new MemberNode("int")), new MemberNode("a"));
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseVariableDefinition(context, out var type);
        parsed.ShouldBe(true);
        type.ShouldBeEquivalentTo(ast);
    }

    [Fact]
    public void ParseVariableDefinitionWithoutName_Incorrect()
    {
        const string code = "int ";
        var exceptionCodes = new List<string>{PlampExceptionInfo.ExpectedVarName().Code};
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseVariableDefinition(context, out _);
        parsed.ShouldBe(false);
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(exceptionCodes);
    }
}