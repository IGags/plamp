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

    [Fact]
    //Парсинг типа с незакрытым пустым дженериком - ошибка
    public void ParseNotClosedEmptyGeneric_ReturnsError()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("List[");
        var res = Parser.TryParseType(context, out var type);
        res.ShouldBeTrue();
        type.ShouldNotBeNull();

        context.Exceptions.Count.ShouldBe(2);
        var codes = new[] { PlampExceptionInfo.GenericArgsIsNotClosed().Code, PlampExceptionInfo.ExpectedGenericArg().Code };
        codes.All(x => context.Exceptions.Any(y => y.Code == x)).ShouldBeTrue();
    }

    [Fact]
    public void ParseEmptyGenericArgs_ReturnsError()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("List[]");
        var res = Parser.TryParseType(context, out var type);
        res.ShouldBeTrue();
        type.ShouldNotBeNull();

        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.EmptyGenericArgs().Code);
    }

    [Fact]
    public void ParseTypeWithSimpleGeneric_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("List[int]");
        var res = Parser.TryParseType(context, out var type);
        res.ShouldBeTrue();
        type.ShouldNotBeNull();
        context.Exceptions.ShouldBeEmpty();
        var genericArg = type.GenericParameters.ShouldHaveSingleItem();
        genericArg.GenericParameters.ShouldBeEmpty();
        genericArg.ArrayDefinitions.ShouldBeEmpty();
        genericArg.TypeName.Name.ShouldBe("int");
    }

    [Fact]
    public void ParseNestedGeneric_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("List[List[int]]");
        var res = Parser.TryParseType(context, out var type);
        res.ShouldBeTrue();
        type.ShouldNotBeNull();
        context.Exceptions.ShouldBeEmpty();

        var genericArg = type.GenericParameters.ShouldHaveSingleItem();
        genericArg.TypeName.Name.ShouldBe("List");
        genericArg.ArrayDefinitions.ShouldBeEmpty();

        genericArg = genericArg.GenericParameters.ShouldHaveSingleItem();
        genericArg.GenericParameters.ShouldBeEmpty();
        genericArg.ArrayDefinitions.ShouldBeEmpty();
        genericArg.TypeName.Name.ShouldBe("int");
    }

    [Fact]
    public void ParseTwoGenericArgs_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("Map[string, int]");
        var res = Parser.TryParseType(context, out var type);
        res.ShouldBeTrue();
        type.ShouldNotBeNull();
        context.Exceptions.ShouldBeEmpty();
        
        type.GenericParameters.Count.ShouldBe(2);
        type.GenericParameters[0].TypeName.Name.ShouldBe("string");
        type.GenericParameters[1].TypeName.Name.ShouldBe("int");
    }

    [Fact]
    public void ParseNotClosedGeneric_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("List[string");
        var res = Parser.TryParseType(context, out var type);
        res.ShouldBeTrue();
        type.ShouldNotBeNull();
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.GenericArgsIsNotClosed().Code);
        type.GenericParameters.ShouldHaveSingleItem();
    }

    [Fact]
    public void ParseNotCompletedGeneric_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("Map[string,]");
        var res = Parser.TryParseType(context, out _);
        res.ShouldBeTrue();
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedGenericArg().Code);
    }

    [Fact]
    public void NotClosedNotCompletedGeneric_Incorrect()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("Map[string,");
        var res = Parser.TryParseType(context, out _);
        res.ShouldBeTrue();
        context.Exceptions.Count.ShouldBe(2);
        var codes = new[] { PlampExceptionInfo.GenericArgsIsNotClosed().Code, PlampExceptionInfo.ExpectedGenericArg().Code };
        codes.All(x => context.Exceptions.Any(y => y.Code == x)).ShouldBeTrue();
    }

    [Fact]
    public void NotCompleteInnerGeneric_ReturnsError()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("Map[List[int, int]");
        var res = Parser.TryParseType(context, out _);
        res.ShouldBeTrue();
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.GenericArgsIsNotClosed().Code);
    }
}