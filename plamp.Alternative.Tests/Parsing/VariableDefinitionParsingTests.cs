using AutoFixture;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class VariableDefinitionParsingTests
{
    [Fact]
    public void ParseVariableDefinition_Correct()
    {
        const string code = "a: int";
        var ast = new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a"));
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseVariableDefinitionSequence(context, out var type);
        context.Exceptions.ShouldBeEmpty();
        parsed.ShouldBe(true);
        type.ShouldBeEquivalentTo(ast);
    }
    
    [Fact]
    public void ParseVariableDefinitionMany_Correct()
    {
        const string code = "a, b, c: int";
        var ast = new VariableDefinitionNode(
            new TypeNode(new TypeNameNode("int")), 
            [new VariableNameNode("a"), new VariableNameNode("b"), new VariableNameNode("c")]);
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseVariableDefinitionSequence(context, out var type);
        context.Exceptions.ShouldBeEmpty();
        parsed.ShouldBe(true);
        type.ShouldBeEquivalentTo(ast);
    }

    [Fact]
    public void ParseArrayTypedVariableDefinition_Correct()
    {
        const string code = "a: []int";
        var ast = new VariableDefinitionNode(
                new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [new ArrayTypeSpecificationNode()] },
                new VariableNameNode("a"));
        
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseVariableDefinitionSequence(context, out var type);
        context.Exceptions.ShouldBeEmpty();
        parsed.ShouldBe(true);
        type.ShouldBeEquivalentTo(ast);
    }

    [Fact]
    public void ParseVariableDefinitionWithoutName_Incorrect()
    {
        const string code = ":int";
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseVariableDefinitionSequence(context, out _);
        parsed.ShouldBe(false);
        context.Exceptions.ShouldBeEmpty();
    }
}