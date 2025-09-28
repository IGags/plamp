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

public class ArrayElementNodeManipulationTests
{
    private AssignNode MakeArrayInitNode(string arrayVarName)
    {
        var varType = new TypeNode(new TypeNameNode("int")) {ArrayDefinitions = [new ArrayTypeSpecificationNode()]};
        var arrayItemType = new TypeNode(new TypeNameNode("int"));
        return new AssignNode(
            new VariableDefinitionNode(varType, new VariableNameNode(arrayVarName)),
            new InitArrayNode(arrayItemType, new LiteralNode(3, typeof(int))));
    }
    
    [Fact]
    public void InferenceArrayTypeInArrayGetter_Correct()
    {
        var arrayName = "a";
        var arrayGetter = new ElemGetterNode(new MemberNode(arrayName),
            new ArrayIndexerNode(new LiteralNode(1, typeof(int))));

        var itemAssign = new AssignNode(new MemberNode("b"), arrayGetter);
        var ast = new BodyNode(
        [
            MakeArrayInitNode(arrayName),
            itemAssign
        ]);
        
        var visitor = new TypeInferenceWeaver();
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();
        
        _ = visitor.WeaveDiffs(ast, context);
        context.Exceptions.ShouldBeEmpty();
        arrayGetter.ItemType.ShouldNotBeNull().ShouldBe(typeof(int));
    }

    [Fact]
    public void InferenceArrayElementGetterFromTypeNotArray_ReturnsError()
    {
        var varType = new TypeNode(new TypeNameNode("int"));
        var def = new VariableDefinitionNode(varType, new VariableNameNode("a"));
        
        var arrayGetter = new ElemGetterNode(new MemberNode("a"),
            new ArrayIndexerNode(new LiteralNode(1, typeof(int))));

        var itemAssign = new AssignNode(new MemberNode("b"), arrayGetter);
        var ast = new BodyNode(
        [
            def,
            itemAssign
        ]);

        var visitor = new TypeInferenceWeaver();
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();

        _ = visitor.WeaveDiffs(ast, context);
        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.IndexerIsNotApplicable().Code);
    }

    [Fact]
    public void InferenceArrayElementGetterFromMultidimArray_ThrowsException()
    {
        var varType = new TypeNode(new TypeNameNode("int"));
        varType.SetType(typeof(int[,]));
        var def = new VariableDefinitionNode(varType, new VariableNameNode("a"));
        
        var arrayGetter = new ElemGetterNode(new MemberNode("a"),
            new ArrayIndexerNode(new LiteralNode(1, typeof(int))));
        var itemAssign = new AssignNode(new MemberNode("b"), arrayGetter);
        var ast = new BodyNode(
        [
            def,
            itemAssign
        ]);
        
        var visitor = new TypeInferenceWeaver();
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();

        Should.Throw<Exception>(() => visitor.WeaveDiffs(ast, context));
    }

    [Fact]
    public void InferenceArrayElementGetterCannotImplicitCast_ReturnsError()
    {
        const string arrName = "a";
        var array = MakeArrayInitNode(arrName);
        var assign = new AssignNode(
            new MemberNode("b"),
            new ElemGetterNode(
                new MemberNode("a"),
                new ArrayIndexerNode(new LiteralNode('a', typeof(char)))));
        var ast = new BodyNode(
        [
            array,
            assign
        ]);
        
        var visitor = new TypeInferenceWeaver();
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();
        _ = visitor.WeaveDiffs(ast, context);
        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.IndexerValueMustBeInteger().Code);
    }

    [Fact]
    public void InferenceArrayTypeInArrayElemSetter_Correct()
    {
        const string arrayName = "a";
        var arrayInit = MakeArrayInitNode(arrayName);
        var setter = new ElemSetterNode(
            new MemberNode("a"),
            new ArrayIndexerNode(new LiteralNode(1, typeof(int))),
            new LiteralNode(1, typeof(int)));

        var ast = new BodyNode(
        [
            arrayInit, 
            setter
        ]);
        
        var visitor = new TypeInferenceWeaver();
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();
        _ = visitor.WeaveDiffs(ast, context);
        context.Exceptions.ShouldBeEmpty();
        setter.ItemType.ShouldNotBeNull().ShouldBe(typeof(int));
    }

    [Fact]
    public void InferenceArrayElemSetterToNotArrayTye_ReturnsError()
    {
        var varType = new TypeNode(new TypeNameNode("int"));
        var def = new VariableDefinitionNode(varType, new VariableNameNode("a"));
        
        var arrayGetter = new ElemSetterNode(new MemberNode("a"),
            new ArrayIndexerNode(new LiteralNode(1, typeof(int))),
            new LiteralNode(1, typeof(int)));

        var itemAssign = new AssignNode(new MemberNode("b"), arrayGetter);
        var ast = new BodyNode(
        [
            def,
            itemAssign
        ]);

        var visitor = new TypeInferenceWeaver();
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();

        _ = visitor.WeaveDiffs(ast, context);
        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.IndexerIsNotApplicable().Code);
    }

    [Fact]
    public void InferenceArraySetterToMultidimArray_ThrowsException()
    {
        var varType = new TypeNode(new TypeNameNode("int"));
        varType.SetType(typeof(int[,]));
        var def = new VariableDefinitionNode(varType, new VariableNameNode("a"));
        
        var arraySetter = new ElemSetterNode(new MemberNode("a"),
            new ArrayIndexerNode(new LiteralNode(1, typeof(int))),
            new LiteralNode(1, typeof(int)));
        var ast = new BodyNode(
        [
            def,
            arraySetter
        ]);
        
        var visitor = new TypeInferenceWeaver();
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();

        Should.Throw<Exception>(() => visitor.WeaveDiffs(ast, context));
    }
    
    [Fact]
    public void InferenceArrayElementSetterCannotImplicitCast_ReturnsError()
    {
        const string arrName = "a";
        var array = MakeArrayInitNode(arrName);
        
        var setter = new ElemSetterNode(
                new MemberNode("a"),
                new ArrayIndexerNode(new LiteralNode('a', typeof(char))),
                new LiteralNode('a', typeof(char)));
        var ast = new BodyNode(
        [
            array,
            setter
        ]);
        
        var visitor = new TypeInferenceWeaver();
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();
        _ = visitor.WeaveDiffs(ast, context);
        var errorCodeShould = new List<string>()
            { PlampExceptionInfo.CannotAssign().Code, PlampExceptionInfo.IndexerValueMustBeInteger().Code };
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodeShould);
    }

    [Fact]
    public void InferenceArrayGetterMakeIndexerImplicitCast_ReturnsCorrect()
    {
        const string arrName = "a";
        var array = MakeArrayInitNode(arrName);
        var arrayGetter = new ElemGetterNode(new MemberNode(arrName),
            new ArrayIndexerNode(new LiteralNode(1, typeof(byte))));

        var itemAssign = new AssignNode(new MemberNode("b"), arrayGetter);
        var ast = new BodyNode(
        [
            array,
            itemAssign
        ]);
        
        var visitor = new TypeInferenceWeaver();
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();
        _ = visitor.WeaveDiffs(ast, context);
        
        context.Exceptions.ShouldBeEmpty();
        ast.ShouldBeOfType<BodyNode>()
            .ExpressionList.ShouldSatisfyAllConditions(
                x => x.Count.ShouldBe(2), 
                x => x[1].ShouldBeOfType<AssignNode>()
                    .Right.ShouldBeOfType<ElemGetterNode>()
                    .ArrayIndexer.IndexMember.ShouldBeOfType<CastNode>()
                    .FromType.ShouldBe(typeof(byte)));
    }

    [Fact]
    public void InferenceArraySetterMakeIndexerImplicitCast_ReturnsCorrect()
    {
        const string arrName = "a";
        var array = MakeArrayInitNode(arrName);
        var arraySetter = new ElemSetterNode(
            new MemberNode(arrName),
            new ArrayIndexerNode(new LiteralNode(1, typeof(byte))),
            new LiteralNode(1, typeof(int)));

        var ast = new BodyNode(
        [
            array,
            arraySetter
        ]);
        
        var visitor = new TypeInferenceWeaver();
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();
        _ = visitor.WeaveDiffs(ast, context);
        
        context.Exceptions.ShouldBeEmpty();
        ast.ShouldBeOfType<BodyNode>()
            .ExpressionList.ShouldSatisfyAllConditions(
                x => x.Count.ShouldBe(2), 
                x => x[1].ShouldBeOfType<ElemSetterNode>()
                    .ArrayIndexer.IndexMember.ShouldBeOfType<CastNode>()
                    .FromType.ShouldBe(typeof(byte)));
    }
}