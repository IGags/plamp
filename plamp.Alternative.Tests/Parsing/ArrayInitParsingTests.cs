using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class ArrayInitParsingTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public void ParseArrayInit_Correct(int length)
    {
        var code = $"[{length}]int";
        var nodeShould = new InitArrayNode(new TypeNode(new TypeNameNode("int")), new LiteralNode(length, typeof(int)));
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseArrayInitialization(context, out var arrayInit);
        result.ShouldBe(true);
        arrayInit.ShouldBeEquivalentTo(nodeShould);
    }

    [Fact]
    public void ParseJaggedArrayInit_Correct()
    {
        const string code = "[3][]long";
        var nodeShould = new InitArrayNode(new TypeNode(new TypeNameNode("long")) {ArrayDefinitions = [new ArrayTypeSpecificationNode()]}, new LiteralNode(3, typeof(int)));
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseArrayInitialization(context, out var arrayInit);
        result.ShouldBe(true);
        arrayInit.ShouldBeEquivalentTo(nodeShould);
    }

    [Fact]
    public void ParseArrayInitWithoutLength_Incorrect()
    {
        const string code = "[]uint";
        var errorCodes = new List<string>{ PlampExceptionInfo.ArrayInitializationMustHasLength().Code };
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseArrayInitialization(context, out _);
        result.ShouldBe(false);
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodes);
    }

    [Fact]
    public void ParseArrayInvalidTypeSpec_Incorrect()
    {
        const string code = "[1]+";
        var errorCodes = new List<string>{ PlampExceptionInfo.ExpectedTypeName().Code };
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseArrayInitialization(context, out _);
        result.ShouldBe(false);
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodes);
    }

    [Fact]
    public void ParseArrayInitWithMemberLength_Correct()
    {
        const string code = "[ln]int";
        var ast = new InitArrayNode(new TypeNode(new TypeNameNode("int")), new MemberNode("ln"));
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseArrayInitialization(context, out var node);
        result.ShouldBe(true);
        node.ShouldBeEquivalentTo(ast);
    }

    [Fact]
    public void ParseArrayInitWithUnaryOperatorLength_Correct()
    {
        const string code = "[++ln]int";
        var ast = new InitArrayNode(new TypeNode(new TypeNameNode("int")), new PrefixIncrementNode(new MemberNode("ln")));
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseArrayInitialization(context, out var node);
        result.ShouldBe(true);
        node.ShouldBeEquivalentTo(ast);
    }

    [Fact]
    public void ParseArrayInitWithBinaryOperatorLength_Correct()
    {
        const string code = "[a + b]int";
        var ast = new InitArrayNode(new TypeNode(new TypeNameNode("int")), new AddNode(new MemberNode("a"), new MemberNode("b")));
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseArrayInitialization(context, out var node);
        result.ShouldBe(true);
        node.ShouldBeEquivalentTo(ast);
    }

    [Fact]
    public void ParseArrayInitWithFuncCallLength_Correct()
    {
        const string code = "[funcCall()]int";
        var ast = new InitArrayNode(new TypeNode(new TypeNameNode("int")), new CallNode(null, new FuncCallNameNode("funcCall"), []));
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseArrayInitialization(context, out var node);
        result.ShouldBe(true);
        node.ShouldBeEquivalentTo(ast);
    }

    [Fact]
    public void ParseArrayInitWithArrayGetterLength_Correct()
    {
        const string code = "[t[1]]int";
        var ast = new InitArrayNode(
            new TypeNode(new TypeNameNode("int")), 
            new ElemGetterNode(new MemberNode("t"), new ArrayIndexerNode(new LiteralNode(1, typeof(int)))));
        
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseArrayInitialization(context, out var node);
        result.ShouldBe(true);
        node.ShouldBeEquivalentTo(ast);
    }
}