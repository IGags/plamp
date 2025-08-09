using System.Collections.Generic;
using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class ReturnTypeInferenceTests
{
    //func void, empty return

    [Theory, AutoData]
    public void UnresolvedFuncReturnType_ReturnNoException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new FuncNode(
            new TypeNode(new MemberNode("abc")),
            new MemberNode("aaa"),
            [],
            new BodyNode([new ReturnNode(null)]));
        SetupMockAndAssertCorrect(ast, symbolTable, fileName, visitor);
    }

    [Theory, AutoData]
    public void ReturnSameTypeAsFunc_ReturnNoException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new MemberNode("int"));
        returnType.SetType(typeof(int));
        var ast = new FuncNode(
            returnType, new MemberNode("aaa"), [],
            new BodyNode([new ReturnNode(new LiteralNode(1, typeof(int)))]));
        SetupMockAndAssertCorrect(ast, symbolTable, fileName, visitor);
    }
    
    [Theory, AutoData]
    public void VoidFuncReturnNull_ReturnNoException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new MemberNode("void"));
        returnType.SetType(typeof(void));
        var ast = new FuncNode(
            returnType, new MemberNode("aaa"), [],
            new BodyNode([new ReturnNode(null)]));
        SetupMockAndAssertCorrect(ast, symbolTable, fileName, visitor);
    }

    [Theory, AutoData]
    public void ReturnDifferentTypeFromFunc_ReturnException([Frozen] Mock<ISymbolTable> symbolTable, string fileName,
        TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new MemberNode("int"));
        returnType.SetType(typeof(int));
        var ast = new FuncNode(
            returnType, new MemberNode("aaa"), [],
            new BodyNode([new ReturnNode(new LiteralNode(1d, typeof(double)))]));
        SetupExceptionMock(symbolTable, fileName);
        var context = new PreCreationContext(fileName, symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.ReturnTypeMismatch().Code));
    }

    [Theory, AutoData]
    public void FuncVoidNodeReturnValue_ReturnException([Frozen] Mock<ISymbolTable> symbolTable,
        string fileName,
        TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new MemberNode("void"));
        returnType.SetType(typeof(void));
        var ast = new FuncNode(
            returnType, new MemberNode("aaa"), [],
            new BodyNode([new ReturnNode(new LiteralNode(1, typeof(int)))]));
        SetupExceptionMock(symbolTable, fileName);
        var context = new PreCreationContext(fileName, symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotReturnValue().Code));
    }
    
    [Theory, AutoData]
    public void FuncHasReturnTypeNodeReturnNull_ReturnException([Frozen] Mock<ISymbolTable> symbolTable,
        string fileName,
        TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new MemberNode("int"));
        returnType.SetType(typeof(int));
        var ast = new FuncNode(
            returnType, new MemberNode("aaa"), [],
            new BodyNode([new ReturnNode(null)]));
        SetupExceptionMock(symbolTable, fileName);
        var context = new PreCreationContext(fileName, symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.ReturnValueIsMissing().Code));
    }

    private void SetupExceptionMock(Mock<ISymbolTable> symbolTable, string fileName)
    {
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        symbolTable.Setup(x => x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>(), fileName))
            .Returns<NodeBase, PlampExceptionRecord, string>((_, b, c) => new PlampException(b, default, default, c));
    }
    
    private void SetupMockAndAssertCorrect(NodeBase ast, Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        var context = new PreCreationContext(fileName, symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldBeEmpty(); 
    }
}