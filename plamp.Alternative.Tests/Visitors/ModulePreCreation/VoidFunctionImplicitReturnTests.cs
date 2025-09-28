using AutoFixture;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.ImplicitReturnInVoid;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation;

public class VoidFunctionImplicitReturnTests
{
    [Fact]
    public void VoidFunctionEmptyBody_AddImplicitReturn()
    {
        const string code = """fn nop() {}""";
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseTopLevel(context, out var expression);
        result.ShouldBe(true);
        var visitor = new ImplicitReturnInVoidFuncWeaver();
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);
        var weaveResult = visitor.WeaveDiffs(expression!, preCreation);
        weaveResult.Exceptions.ShouldBeEmpty();
        expression.ShouldBeOfType<FuncNode>()
            .Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<ReturnNode>()
            .ReturnValue.ShouldBeNull();
    }

    [Fact]
    public void VoidFunctionWithNotNullVoidType_AddImplicitReturn()
    {
        const string code = """fn nop() {}""";
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseTopLevel(context, out var expression);
        result.ShouldBe(true);
        var fn = expression.ShouldBeOfType<FuncNode>();
        var type = new TypeNode(new TypeNameNode("void"));
        type.SetType(typeof(void));
        expression = new FuncNode(type, fn.FuncName, fn.ParameterList, fn.Body);
        var visitor = new ImplicitReturnInVoidFuncWeaver();
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);
        var weaveResult = visitor.WeaveDiffs(expression, preCreation);
        weaveResult.Exceptions.ShouldBeEmpty();
        expression.ShouldBeOfType<FuncNode>()
            .Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<ReturnNode>()
            .ReturnValue.ShouldBeNull();
    }

    [Fact]
    public void VoidFunctionWithReturn_DoesNothing()
    {
        const string code = """fn nop() { return; }""";
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseTopLevel(context, out var expression);
        result.ShouldBe(true);
        var visitor = new ImplicitReturnInVoidFuncWeaver();
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);
        var weaveResult = visitor.WeaveDiffs(expression!, preCreation);
        weaveResult.Exceptions.ShouldBeEmpty();
        expression.ShouldBeOfType<FuncNode>()
            .Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<ReturnNode>()
            .ReturnValue.ShouldBeNull();
    }

    [Fact]
    public void NonVoidFunctionEmptyBody_DoesNothing()
    {
        const string code = "fn nop() int {}";
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseTopLevel(context, out var expression);
        result.ShouldBe(true);
        var visitor = new ImplicitReturnInVoidFuncWeaver();
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);
        var weaveResult = visitor.WeaveDiffs(expression!, preCreation);
        weaveResult.Exceptions.ShouldBeEmpty();
        expression.ShouldBeOfType<FuncNode>()
            .Body.ExpressionList.ShouldBeEmpty();
    }
}