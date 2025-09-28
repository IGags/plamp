using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class TypeParsingTests
{
    [Fact]
    public void ParseType_Correct()
    {
        const string code = "int";
        var ast = new TypeNode(new TypeNameNode("int"));
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseType(context, out var type);
        context.Exceptions.ShouldBeEmpty();
        parsed.ShouldBe(true);
        type.ShouldBeEquivalentTo(ast);
    }

    [Fact]
    public void ParseType_Incorrect()
    {
        const string code = "1";
        var exceptionCodes = new List<string>{PlampExceptionInfo.ExpectedTypeName().Code};
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseType(context, out _);
        parsed.ShouldBe(false);
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(exceptionCodes);
    }

    [Fact]
    public void ParseFlatArrayType_Correct()
    {
        const string code = "[]int";
        var ast = new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [new ArrayTypeSpecificationNode()]};
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseType(context, out var type);
        context.Exceptions.ShouldBeEmpty();
        parsed.ShouldBe(true);
        type.ShouldBeEquivalentTo(ast);
    }

    [Fact]
    public void ParseJaggedArrayType_Correct()
    {
        const string code = "[][][]string";
        var ast = new TypeNode(new TypeNameNode("string"))
                { ArrayDefinitions = Enumerable.Repeat(new ArrayTypeSpecificationNode(), 3).ToList() };
        
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseType(context, out var type);
        context.Exceptions.ShouldBeEmpty();
        parsed.ShouldBe(true);
        type.ShouldBeEquivalentTo(ast);
    }

    [Theory]
    [InlineData("[int")]
    [InlineData("[][int")]
    [InlineData("[[]int")]
    public void ParseNotClosedArrayType_Incorrect(string code)
    {
        var exceptionCodes = new List<string>{PlampExceptionInfo.ArrayDefinitionIsNotClosed().Code};
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseType(context, out _);
        parsed.ShouldBe(false);
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(exceptionCodes);
    }
}