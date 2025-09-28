using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference.Util;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class ArrayInitInferenceTests
{
    [Fact]
    public void InitArrayInferenceType_Correct()
    {
        var assign = new AssignNode(
            new MemberNode("a"),
            new InitArrayNode(
                new TypeNode(new TypeNameNode("int")),
                new LiteralNode(3, typeof(int))));

        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();
        var visitor = new TypeInferenceWeaver();
        _ = visitor.WeaveDiffs(assign, context);
        
        context.Exceptions.ShouldBeEmpty();
        assign.Right.ShouldBeOfType<InitArrayNode>()
            .ArrayItemType.Symbol.ShouldNotBeNull().ShouldBe(typeof(int));
    }

    [Fact]
    public void InitArrayInvalidLength_ReturnsError()
    {
        var assign = new AssignNode(
            new MemberNode("a"),
            new InitArrayNode(
                new TypeNode(new TypeNameNode("int")),
                new MemberNode("biba")));
        
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();
        var visitor = new TypeInferenceWeaver();
        _ = visitor.WeaveDiffs(assign, context);

        var errorCodesShould = new List<string>()
        {
            PlampExceptionInfo.CannotFindMember().Code,
            PlampExceptionInfo.ArrayInitializationMustHasLength().Code
        };
        
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodesShould);
    }

    [Fact]
    public void InitArrayImplicitCastLength_Correct()
    {
        var assign = new AssignNode(
            new MemberNode("a"),
            new InitArrayNode(
                new TypeNode(new TypeNameNode("int")),
                new LiteralNode(1, typeof(byte))));
        
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();
        var visitor = new TypeInferenceWeaver();
        _ = visitor.WeaveDiffs(assign, context);
        
        context.Exceptions.ShouldBeEmpty();
        assign.Right.ShouldBeOfType<InitArrayNode>()
            .LengthDefinition.ShouldBeOfType<CastNode>()
            .FromType.ShouldBe(typeof(byte));
    }

    [Fact]
    public void InitArrayImplicitCastLength_Incorrect()
    {
        var assign = new AssignNode(
            new MemberNode("a"),
            new InitArrayNode(
                new TypeNode(new TypeNameNode("int")),
                new LiteralNode(1, typeof(long))));
        
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();
        var visitor = new TypeInferenceWeaver();
        _ = visitor.WeaveDiffs(assign, context);

        var errorCodeShould = PlampExceptionInfo.ArrayLengthMustBeInteger().Code;
        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(errorCodeShould);
    }

    [Fact]
    public void InitArrayVariableImplicitEmptyAssign_Correct()
    {
        var def = new VariableDefinitionNode(
            new TypeNode(new TypeNameNode("int"))
                { ArrayDefinitions = [new ArrayTypeSpecificationNode(), new ArrayTypeSpecificationNode()] },
            new VariableNameNode("a"));

        var body = new BodyNode([def]);
        
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();
        var visitor = new TypeInferenceWeaver();
        _ = visitor.WeaveDiffs(body, context);
        
        context.Exceptions.ShouldBeEmpty();
        var assign = body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<AssignNode>();
        
        assign.Left.ShouldBeOfType<VariableDefinitionNode>()
            .Type.ShouldNotBeNull().Symbol.ShouldBe(typeof(int[][]));
        
        var call = assign.Right.ShouldBeOfType<CallNode>();
        call.From.ShouldBeNull();
        call.Name.Value.ShouldBe("__FROM_C#__ARRAY::Empty<T>");
        call.Args.ShouldBeEmpty();
        
        var methodInfo = typeof(Array).GetMethod(nameof(Array.Empty));
        var infoConstructed = methodInfo!.MakeGenericMethod(typeof(int[]));
        call.Symbol.ShouldNotBeNull().ShouldBe(infoConstructed);
    }
}