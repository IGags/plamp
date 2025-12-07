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
using plamp.Intrinsics;
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
            [new VariableDefinitionNode(varType, new VariableNameNode(arrayVarName))],
            [new InitArrayNode(arrayItemType, new LiteralNode(3, RuntimeSymbols.SymbolTable.MakeInt()))]
        );
    }
    
    [Fact]
    public void InferenceArrayTypeInArrayGetter_Correct()
    {
        var arrayName = "a";
        var arrayGetter = 
            new IndexerNode(new MemberNode(arrayName), new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeInt()));

        var itemAssign = new AssignNode([new MemberNode("b")], [arrayGetter]);
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
        arrayGetter.ItemType.ShouldNotBeNull().ShouldBe(RuntimeSymbols.SymbolTable.MakeInt());
    }

    [Fact]
    public void InferenceArrayElementGetterFromTypeNotArray_ReturnsError()
    {
        var varType = new TypeNode(new TypeNameNode("int"));
        var def = new VariableDefinitionNode(varType, new VariableNameNode("a"));
        
        var arrayGetter = 
            new IndexerNode(new MemberNode("a"), new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeInt()));

        var itemAssign = new AssignNode([new MemberNode("b")], [arrayGetter]);
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
    public void InferenceArrayElementGetterCannotImplicitCast_ReturnsError()
    {
        const string arrName = "a";
        var array = MakeArrayInitNode(arrName);
        var assign = new AssignNode(
            [new MemberNode("b")],
            [new IndexerNode(new MemberNode("a"), new LiteralNode('a', RuntimeSymbols.SymbolTable.MakeChar()))]
        );
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
        var setter = new AssignNode(
            [new IndexerNode(new MemberNode("a"), new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeInt()))],
            [new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeInt())]
        );

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
        setter.Targets.ShouldHaveSingleItem().ShouldBeOfType<IndexerNode>().ItemType.ShouldNotBeNull().ShouldBe(RuntimeSymbols.SymbolTable.MakeInt());
    }

    [Fact]
    public void InferenceArrayElemSetterToNotArrayType_ReturnsError()
    {
        var varType = new TypeNode(new TypeNameNode("int"));
        var def = new VariableDefinitionNode(varType, new VariableNameNode("a"));
        
        var assign = new AssignNode(
            [new IndexerNode(new MemberNode("a"), new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeInt()))],
            [new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeInt())]
        );

        var ast = new BodyNode(
        [
            def,
            assign
        ]);

        var visitor = new TypeInferenceWeaver();
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();

        _ = visitor.WeaveDiffs(ast, context);
        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.IndexerIsNotApplicable().Code);
    }
    
    [Fact]
    public void InferenceArrayElementSetterCannotImplicitCast_ReturnsError()
    {
        const string arrName = "a";
        var array = MakeArrayInitNode(arrName);
        
        var setter = new AssignNode(
                [new IndexerNode(new MemberNode("a"), new LiteralNode('a', RuntimeSymbols.SymbolTable.MakeChar()))],
                [new LiteralNode('a', RuntimeSymbols.SymbolTable.MakeChar())]
        );
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
            { PlampExceptionInfo.IndexerValueMustBeInteger().Code, PlampExceptionInfo.CannotAssign().Code };
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodeShould);
    }

    [Fact]
    public void InferenceArrayGetterMakeIndexerImplicitCast_ReturnsCorrect()
    {
        const string arrName = "a";
        var array = MakeArrayInitNode(arrName);
        var arrayGetter = new IndexerNode(new MemberNode(arrName), new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeByte()));

        var itemAssign = new AssignNode([new MemberNode("b")], [arrayGetter]);
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
                    .Sources.ShouldHaveSingleItem().ShouldBeOfType<IndexerNode>()
                    .IndexMember.ShouldBeOfType<CastNode>()
                    .FromType.ShouldBe(RuntimeSymbols.SymbolTable.MakeByte()));
    }

    [Fact]
    public void InferenceArraySetterMakeIndexerImplicitCast_ReturnsCorrect()
    {
        const string arrName = "a";
        var array = MakeArrayInitNode(arrName);
        var arraySetter = new AssignNode(
            [new IndexerNode(new MemberNode(arrName), new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeByte()))],
            [new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeInt())]
        );

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
                x => x[1].ShouldBeOfType<AssignNode>()
                    .Sources.ShouldHaveSingleItem()
                    .ShouldBeOfType<LiteralNode>()
                    .Value.ShouldBe(1));
    }
}