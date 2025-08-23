using System.Collections.Generic;
using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

//TODO: A lot of duplicate code need to configure AutoFixture correct
public class OperatorTypeInferenceTests
{
    [Theory, AutoData]
    public void UnaryLogicalWithMatchInner_ReturnsNoException([Frozen]Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new NotNode(new LiteralNode(true, typeof(bool)));
        SetupMockAndAssertCorrect(ast, symbolTable, fileName, visitor);
    }

    [Theory, AutoData]
    public void UnaryLogicalWithMismatchInner_ReturnsException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new NotNode(new LiteralNode(1, typeof(int)));
        SetupMockAndAssertError(ast, symbolTable, fileName, visitor);
    }

    [Theory, AutoData]
    public void UnaryArithmeticWithMatchInner_ReturnsNoException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new PrefixIncrementNode(new LiteralNode(1, typeof(int)));
        SetupMockAndAssertCorrect(ast, symbolTable, fileName, visitor);
    }

    [Theory, AutoData]
    public void UnaryArithmeticWithMismatchInner_ReturnsException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new PrefixIncrementNode(new LiteralNode(true, typeof(bool)));
        SetupMockAndAssertError(ast, symbolTable, fileName, visitor);
    }

    [Theory, AutoData]
    public void BinaryLogicalGateWithMatchInner_ReturnsNoException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new AndNode(new LiteralNode(true, typeof(bool)), new LiteralNode(false, typeof(bool)));
        SetupMockAndAssertCorrect(ast, symbolTable, fileName, visitor);
    }

    [Theory, AutoData]
    public void BinaryLogicalWithMismatchFirst_ReturnsException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new OrNode(new LiteralNode(1.4, typeof(float)), new LiteralNode(true, typeof(bool)));
        SetupMockAndAssertError(ast, symbolTable, fileName, visitor);
    }

    [Theory, AutoData]
    public void BinaryLogicalWithMismatchSecond_ReturnsException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new OrNode(new LiteralNode(true, typeof(bool)), new LiteralNode(1.4, typeof(float)));
        SetupMockAndAssertError(ast, symbolTable, fileName, visitor);
    }

    [Theory, AutoData]
    public void BinaryLogicalWithMismatchBoth_ReturnsException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new OrNode(new LiteralNode(1.4, typeof(float)), new LiteralNode(1.4, typeof(float)));
        SetupMockAndAssertError(ast, symbolTable, fileName, visitor);
    }

    [Theory, AutoData]
    public void BinaryComparisionDifferentType_ReturnsException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new LessNode(new LiteralNode(1, typeof(int)), new LiteralNode(1.4, typeof(float)));
        SetupMockAndAssertCorrect(ast, symbolTable, fileName, visitor);
    }

    [Theory, AutoData]
    public void BinaryComparisionSameType_ReturnsNoException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new GreaterNode(new LiteralNode(1, typeof(int)), new LiteralNode(0, typeof(int)));
        SetupMockAndAssertCorrect(ast, symbolTable, fileName, visitor);
    }

    [Theory, AutoData]
    public void BinaryArithmeticalDifferentType_ReturnsException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new AddNode(new LiteralNode(2, typeof(int)), new LiteralNode(true, typeof(bool)));
        SetupMockAndAssertError(ast, symbolTable, fileName, visitor);
    }

    [Theory, AutoData]
    public void BinaryArithmeticalSameType_ReturnsNoException([Frozen] Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new DivNode(new LiteralNode(1, typeof(int)), new LiteralNode(0, typeof(int)));
        SetupMockAndAssertCorrect(ast, symbolTable, fileName, visitor);
    }

    private void SetupMockAndAssertCorrect(NodeBase ast, Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        var context = new PreCreationContext(fileName, symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldBeEmpty(); 
    }
    
    private void SetupMockAndAssertError(NodeBase ast, Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        symbolTable.Setup(x => x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>(), fileName))
            .Returns<NodeBase, PlampExceptionRecord, string>((_, b, c) => new PlampException(b, default, default, c));
        
        var context = new PreCreationContext(fileName, symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.CannotApplyOperator().Code);
    }
}