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
    public void CallVoid_ReturnsCorrect([Frozen]Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new CallNode(null, new FuncCallNameNode("a"), [])
        ]);
        
        var retType = new TypeNode(new TypeNameNode("void"));
        retType.SetType(typeof(void));
        var def = new FuncNode(retType, new FuncNameNode("a"), [], new BodyNode([]));
        var funcDict = new Dictionary<string, FuncNode>()
        {
            ["a"] = def
        };
        SetupMocksAndAssertCorrect(ast, symbolTable, fileName, visitor, funcDict);
    }

    [Theory, AutoData]
    public void CallRetType_ReturnsCorrect([Frozen] Mock<ISymbolTable> symbolTable, string fileName,
        TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new CallNode(null, new FuncCallNameNode("a"), [])
        ]);

        var retType = new TypeNode(new TypeNameNode("int"));
        retType.SetType(typeof(int));
        var def = new FuncNode(retType, new FuncNameNode("a"), [], new BodyNode([]));
        var funcDict = new Dictionary<string, FuncNode>()
        {
            ["a"] = def
        };
        SetupMocksAndAssertCorrect(ast, symbolTable, fileName, visitor, funcDict);
    }

    [Theory, AutoData]
    public void CallWithArgs_ReturnsCorrect([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new CallNode(null, new FuncCallNameNode("a"), [new LiteralNode(1, typeof(int)), new LiteralNode("hi", typeof(string))])
        ]);
        var retType = new TypeNode(new TypeNameNode("void"));
        var firstArgType = new TypeNode(new TypeNameNode("int"));
        var secondArgType = new TypeNode(new TypeNameNode("string"));
        retType.SetType(typeof(void));
        firstArgType.SetType(typeof(int));
        secondArgType.SetType(typeof(string));
        
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
        SetupMocksAndAssertCorrect(ast, symbolTable, fileName, visitor, funcDict);
    }

    [Theory, AutoData]
    public void AssignCallVoid_ReturnsException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode(new MemberNode("b"), new CallNode(null, new FuncCallNameNode("a"), []))
        ]);
        
        var retType = new TypeNode(new TypeNameNode("void"));
        retType.SetType(typeof(void));
        var def = new FuncNode(retType, new FuncNameNode("a"), [], new BodyNode([]));
        var funcDict = new Dictionary<string, FuncNode>()
        {
            ["a"] = def
        };
        
        SetupExceptionGenerationMock(symbolTable, fileName);
        var context = new PreCreationContext(fileName, symbolTable.Object);
        foreach (var kvp in funcDict)
        {
            context.Functions.Add(kvp.Key, kvp.Value);
        }
        
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotAssignNone().Code));
    }

    [Theory, AutoData]
    public void CallNotFullArgs_ReturnException([Frozen] Mock<ISymbolTable> symbolTable, string fileName,
        TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new CallNode(null, new FuncCallNameNode("a"), [new MemberNode("c"), new LiteralNode("hi", typeof(string))])
        ]);
        var retType = new TypeNode(new TypeNameNode("void"));
        var firstArgType = new TypeNode(new TypeNameNode("int"));
        var secondArgType = new TypeNode(new TypeNameNode("string"));
        retType.SetType(typeof(void));
        firstArgType.SetType(typeof(int));
        secondArgType.SetType(typeof(string));
        
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
        
        SetupExceptionGenerationMock(symbolTable, fileName);
        var context = new PreCreationContext(fileName, symbolTable.Object);
        foreach (var kvp in funcDict)
        {
            context.Functions.Add(kvp.Key, kvp.Value);
        }
        
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.Count.ShouldBe(2),
            x => x.Exceptions.Select(y => y.Code).ShouldContain(PlampExceptionInfo.UnknownFunction().Code),
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
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);

        var retType = new TypeNode(new TypeNameNode("void"));
        retType.SetType(typeof(void));
        var argType = new TypeNode(new TypeNameNode("any"));
        argType.SetType(typeof(object));
        
        var mockFuncDef = new FuncNode(
            retType,
            new FuncNameNode("mock"),
            [
                new ParameterNode(argType, new ParameterNameNode("a"))
            ], new BodyNode([]));
        
        preCreation.Functions.Add("mock", mockFuncDef);
        var weaveResult = visitor.WeaveDiffs(expression!, preCreation);
        weaveResult.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    private void CallWithExpandableType_Correct()
    {
        const string code = "mock(1i);";
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseStatement(context, out var expression);
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);

        var retType = new TypeNode(new TypeNameNode("void"));
        retType.SetType(typeof(void));
        var argType = new TypeNode(new TypeNameNode("long"));
        argType.SetType(typeof(long));
        
        var mockFuncDef = new FuncNode(
            retType,
            new FuncNameNode("mock"),
            [
                new ParameterNode(argType, new ParameterNameNode("a"))
            ], new BodyNode([]));
        
        preCreation.Functions.Add("mock", mockFuncDef);
        var weaveResult = visitor.WeaveDiffs(expression!, preCreation);
        weaveResult.Exceptions.ShouldBeEmpty();
    }
    
    private void SetupExceptionGenerationMock(Mock<ISymbolTable> symbolTable, string fileName)
    {
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        symbolTable.Setup(x => x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>(), fileName))
            .Returns<NodeBase, PlampExceptionRecord, string>((_, b, c) => new PlampException(b, default, default, c));
    }
    
    private void SetupMocksAndAssertCorrect(
        NodeBase ast, 
        Mock<ISymbolTable> symbolTable, 
        string fileName, 
        TypeInferenceWeaver visitor, 
        Dictionary<string, FuncNode> funcs)
    {
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        var context = new PreCreationContext(fileName, symbolTable.Object);
        foreach (var kvp in funcs)
        {
            context.Functions.Add(kvp.Key, kvp.Value);
        }
        
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldBeEmpty();
    }
}