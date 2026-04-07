using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Parsing;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
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
        
        var retType = new TypeNode(new TypeNameNode("void"))
        {
            TypeInfo = Builtins.Void
        };
        var def = new FuncNode(retType, new FuncNameNode("a"), [], [], new BodyNode([]));
        var funcDict = new Dictionary<string, FuncNode>
        {
            ["a"] = def
        };
        SetupMocksAndAssertCorrect(ast, translationTable, new SymTableBuilder(){ModuleName = "mod"}, visitor, funcDict);
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

        var retType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };
        var def = new FuncNode(retType, new FuncNameNode("a"), [], [], new BodyNode([]));
        var funcDict = new Dictionary<string, FuncNode>()
        {
            ["a"] = def
        };
        SetupMocksAndAssertCorrect(ast, translationTable, new SymTableBuilder(){ModuleName = "mod"}, visitor, funcDict);
    }

    [Theory, AutoData]
    public void CallWithArgs_ReturnsCorrect([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new CallNode(null, new FuncCallNameNode("a"), [new LiteralNode(1, Builtins.Int), new LiteralNode("hi", Builtins.String)])
        ]);
        var retType = new TypeNode(new TypeNameNode("void"));
        var firstArgType = new TypeNode(new TypeNameNode("int"));
        var secondArgType = new TypeNode(new TypeNameNode("string"));
        retType.TypeInfo = Builtins.Void;
        firstArgType.TypeInfo = Builtins.Int;
        secondArgType.TypeInfo = Builtins.String;
        
        var def = new FuncNode(
            retType, 
            new FuncNameNode("a"),
            [],
            [
                new ParameterNode(firstArgType, new ParameterNameNode("f")), 
                new ParameterNode(secondArgType, new ParameterNameNode("s"))
            ], 
            new BodyNode([]));
        var funcDict = new Dictionary<string, FuncNode>()
        {
            ["a"] = def
        };
        SetupMocksAndAssertCorrect(ast, translationTable, new SymTableBuilder{ ModuleName = "mod" }, visitor, funcDict);
    }

    [Theory, AutoData]
    public void AssignCallVoid_ReturnsException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("b")], [new CallNode(null, new FuncCallNameNode("a"), [])])
        ]);
        
        var retType = new TypeNode(new TypeNameNode("void"))
        {
            TypeInfo = Builtins.Void
        };
        var def = new FuncNode(retType, new FuncNameNode("a"), [], [], new BodyNode([]));
        var funcDict = new Dictionary<string, FuncNode>()
        {
            ["a"] = def
        };
        
        SetupExceptionGenerationMock(translationTable);
        var currentModule = new SymTableBuilder();
        var context = new PreCreationContext(translationTable.Object, [currentModule]);
        foreach (var kvp in funcDict)
        {
            currentModule.DefineFunc(kvp.Value);
        }
        
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotAssignNone().Code));
    }

    [Theory, AutoData]
    public void CallNotFullArgs_ReturnExceptionFuncFunc([Frozen] Mock<ITranslationTable> translationTable,
        TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new CallNode(null, new FuncCallNameNode("a"), [new MemberNode("c"), new LiteralNode("hi", Builtins.String)])
        ]);
        var retType = new TypeNode(new TypeNameNode("void"));
        var firstArgType = new TypeNode(new TypeNameNode("int"));
        var secondArgType = new TypeNode(new TypeNameNode("string"));
        retType.TypeInfo = Builtins.Void;
        firstArgType.TypeInfo = Builtins.Int;
        secondArgType.TypeInfo = Builtins.String;
        
        SetupExceptionGenerationMock(translationTable);
        var symbolTable = new SymTableBuilder();
        symbolTable.DefineFunc(new FuncNode(retType, new FuncNameNode("a"),
            [],
            [new(firstArgType, new("first")), new(secondArgType, new("second"))], new([])));
        
        var context = new PreCreationContext(translationTable.Object, [symbolTable, Builtins.SymTable]);
        
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.Count.ShouldBe(1),
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
        var symbolTable = new SymTableBuilder();
        var retType = new TypeNode(new TypeNameNode("void"))
        {
            TypeInfo = Builtins.Void
        };
        var argType = new TypeNode(new TypeNameNode("any"))
        {
            TypeInfo = Builtins.Any
        };
        
        symbolTable.DefineFunc(new FuncNode(retType, new FuncNameNode("mock"), [], [new(argType, new("first"))], new([])));
        var preCreation = new PreCreationContext(context.TranslationTable, [symbolTable, Builtins.SymTable]);

        
        
        var weaveResult = visitor.WeaveDiffs(new BodyNode(expression), preCreation);
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
        var symbolTable = new SymTableBuilder();
        
        var retType = new TypeNode(new TypeNameNode("void"))
        {
            TypeInfo = Builtins.Void
        };
        var argType = new TypeNode(new TypeNameNode("long"))
        {
            TypeInfo = Builtins.Long
        };

        symbolTable.DefineFunc(new FuncNode(retType, new FuncNameNode("mock"), [], [new(argType, new("first"))], new ([])));
        
        var preCreation = new PreCreationContext(context.TranslationTable, [symbolTable]);
        var weaveResult = visitor.WeaveDiffs(new BodyNode(expression), preCreation);
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
        SymTableBuilder symbolTable,
        TypeInferenceWeaver visitor, 
        Dictionary<string, FuncNode> funcs)
    {
        var filePosition = new FilePosition();
        translationTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        var context = new PreCreationContext(translationTable.Object, [symbolTable]);
        foreach (var kvp in funcs)
        {
            if (kvp.Value.ReturnType.TypeInfo == null) throw new Exception();
            symbolTable.DefineFunc(kvp.Value);
        }
        
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldBeEmpty();
    }
}