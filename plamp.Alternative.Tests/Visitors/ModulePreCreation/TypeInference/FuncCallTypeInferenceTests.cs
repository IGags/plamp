using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using plamp.Intrinsics;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class FuncCallTypeInferenceTests
{
    //Inference call not all required args
    [Theory, AutoData]
    public void CallVoid_ReturnsCorrect(
        [Frozen]Mock<ITranslationTable> translationTable,
        TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new CallNode(null, new FuncCallNameNode("a"), [])
        ]);
        
        var retType = new TypeNode(new TypeNameNode("void"));
        retType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeVoid());
        var def = new FuncNode(retType, new FuncNameNode("a"), [], new BodyNode([]));
        var funcDict = new Dictionary<string, FuncNode>
        {
            ["a"] = def
        };
        SetupMocksAndAssertCorrect(ast, translationTable, new SymbolTable("mod", []), visitor, funcDict);
    }

    [Theory, AutoData]
    public void CallRetType_ReturnsCorrect(
        [Frozen] Mock<ITranslationTable> translationTable,
        TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new CallNode(null, new FuncCallNameNode("a"), [])
        ]);

        var retType = new TypeNode(new TypeNameNode("int"));
        retType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeInt());
        var def = new FuncNode(retType, new FuncNameNode("a"), [], new BodyNode([]));
        var funcDict = new Dictionary<string, FuncNode>()
        {
            ["a"] = def
        };
        SetupMocksAndAssertCorrect(ast, translationTable, new SymbolTable("mod", []), visitor, funcDict);
    }

    [Theory, AutoData]
    public void CallWithArgs_ReturnsCorrect([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new CallNode(null, new FuncCallNameNode("a"), [new LiteralNode(1, RuntimeSymbols.GetSymbolTable.MakeInt()), new LiteralNode("hi", RuntimeSymbols.GetSymbolTable.MakeString())])
        ]);
        var retType = new TypeNode(new TypeNameNode("void"));
        var firstArgType = new TypeNode(new TypeNameNode("int"));
        var secondArgType = new TypeNode(new TypeNameNode("string"));
        retType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeVoid());
        firstArgType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeInt());
        secondArgType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeString());
        
        var def = new FuncNode(
            retType, 
            new FuncNameNode("a"), 
            [
                new ParameterNode(firstArgType, new ParameterNameNode("f")), 
                new ParameterNode(secondArgType, new ParameterNameNode("s"))
            ], 
            new BodyNode([]));
        var funcDict = new Dictionary<string, FuncNode>()
        {
            ["a"] = def
        };
        SetupMocksAndAssertCorrect(ast, translationTable, new SymbolTable("mod", []), visitor, funcDict);
    }

    [Theory, AutoData]
    public void AssignCallVoid_ReturnsException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("b")], [new CallNode(null, new FuncCallNameNode("a"), [])])
        ]);
        
        var retType = new TypeNode(new TypeNameNode("void"));
        retType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeVoid());
        var def = new FuncNode(retType, new FuncNameNode("a"), [], new BodyNode([]));
        var funcDict = new Dictionary<string, FuncNode>()
        {
            ["a"] = def
        };
        
        SetupExceptionGenerationMock(translationTable);
        var context = new PreCreationContext(translationTable.Object, new SymbolTable("mod", []));
        foreach (var kvp in funcDict)
        {
            context.SymbolTable.TryAddFunc(kvp.Key, kvp.Value.ReturnType!.TypedefRef!,
                kvp.Value.ParameterList.Select(x => x.Type.TypedefRef).Cast<ICompileTimeType>().ToList(), default, out _);
        }
        
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotAssignNone().Code));
    }

    [Theory, AutoData]
    public void CallNotFullArgs_ReturnException([Frozen] Mock<ITranslationTable> symbolTable,
        TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new CallNode(null, new FuncCallNameNode("a"), [new MemberNode("c"), new LiteralNode("hi", RuntimeSymbols.GetSymbolTable.MakeString())])
        ]);
        var retType = new TypeNode(new TypeNameNode("void"));
        var firstArgType = new TypeNode(new TypeNameNode("int"));
        var secondArgType = new TypeNode(new TypeNameNode("string"));
        retType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeVoid());
        firstArgType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeInt());
        secondArgType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeString());
        
        var def = new FuncNode(
            retType, 
            new FuncNameNode("a"), 
            [
                new ParameterNode(firstArgType, new ParameterNameNode("f")), 
                new ParameterNode(secondArgType, new ParameterNameNode("s"))
            ],
            new BodyNode([]));
        var funcDict = new Dictionary<string, FuncNode>()
        {
            ["a"] = def
        };
        
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object, new SymbolTable("mod", []));
        foreach (var kvp in funcDict)
        {
            context.SymbolTable.TryAddFunc(kvp.Key, kvp.Value.ReturnType!.TypedefRef!,
                kvp.Value.ParameterList.Select(x => x.Type.TypedefRef).Cast<ICompileTimeType>().ToList(), default, out _);
        }
        
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.Count.ShouldBe(2),
            x => x.Exceptions.Select(y => y.Code).ShouldContain(PlampExceptionInfo.FunctionIsNotFound("a", [])),
            x => x.Exceptions.Select(y => y.Code).ShouldContain(PlampExceptionInfo.CannotFindMember().Code));
    }

    [Fact]
    private void CallWithFunctionWithAnyTypeArgument_Correct()
    {
        const string code = "mock(1);";
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseStatement(context, out var expression);
        result.ShouldBe(true);
        expression.ShouldNotBeNull();
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.TranslationTable, new SymbolTable("mod", []));

        var retType = new TypeNode(new TypeNameNode("void"));
        retType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeVoid());
        var argType = new TypeNode(new TypeNameNode("any"));
        argType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeAny());
        
        preCreation.SymbolTable.TryAddFunc("mock", retType.TypedefRef!, [argType.TypedefRef!], default, out _);
        var weaveResult = visitor.WeaveDiffs(expression, preCreation);
        weaveResult.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    private void CallWithExpandableType_Correct()
    {
        const string code = "mock(1i);";
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseStatement(context, out var expression);
        expression.ShouldNotBeNull();
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.TranslationTable, new SymbolTable("mod", []));

        var retType = new TypeNode(new TypeNameNode("void"));
        retType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeVoid());
        var argType = new TypeNode(new TypeNameNode("long"));
        argType.SetTypeRef(RuntimeSymbols.GetSymbolTable.MakeLong());
        
        preCreation.SymbolTable.TryAddFunc("mock", retType.TypedefRef!, [argType.TypedefRef!], default, out _);
        var weaveResult = visitor.WeaveDiffs(expression, preCreation);
        weaveResult.Exceptions.ShouldBeEmpty();
    }
    
    private void SetupExceptionGenerationMock(Mock<ITranslationTable> symbolTable)
    {
        var filePosition = new FilePosition();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        symbolTable.Setup(x => x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>()))
            .Returns<NodeBase, PlampExceptionRecord>((_, b) => new PlampException(b, default));
    }
    
    private void SetupMocksAndAssertCorrect(
        NodeBase ast, 
        Mock<ITranslationTable> translationTable, 
        SymbolTable symbolTable,
        TypeInferenceWeaver visitor, 
        Dictionary<string, FuncNode> funcs)
    {
        var filePosition = new FilePosition();
        translationTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        var context = new PreCreationContext(translationTable.Object, symbolTable);
        foreach (var kvp in funcs)
        {
            if (kvp.Value.ReturnType?.TypedefRef == null) throw new Exception();
            symbolTable.TryAddFunc(kvp.Key, kvp.Value.ReturnType.TypedefRef,
                kvp.Value.ParameterList.Select(x => x.Type.TypedefRef).Cast<ICompileTimeType>().ToList(), default, out _);
        }
        
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldBeEmpty();
    }
}