using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using plamp.Intrinsics;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class OperatorTypeInferenceTests
{
    [Theory, AutoData]
    public void UnaryLogicalWithMatchInner_ReturnsNoException([Frozen]Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new NotNode(new LiteralNode(true, RuntimeSymbols.SymbolTable.Bool));
        SetupMockAndAssertCorrect(ast, translationTable, visitor);
    }

    [Theory, AutoData]
    public void UnaryLogicalWithMismatchInner_ReturnsException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new NotNode(new LiteralNode(1, RuntimeSymbols.SymbolTable.Int));
        SetupMockAndAssertError(ast, translationTable, visitor);
    }

    [Theory, AutoData]
    public void UnaryArithmeticWithMatchInner_ReturnsNoException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new PrefixIncrementNode(new LiteralNode(1, RuntimeSymbols.SymbolTable.Int));
        SetupMockAndAssertCorrect(ast, translationTable, visitor);
    }

    [Theory, AutoData]
    public void UnaryArithmeticWithMismatchInner_ReturnsException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new PrefixIncrementNode(new LiteralNode(true, RuntimeSymbols.SymbolTable.Bool));
        SetupMockAndAssertError(ast, translationTable, visitor);
    }

    [Theory, AutoData]
    public void BinaryLogicalGateWithMatchInner_ReturnsNoException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new AndNode(new LiteralNode(true, RuntimeSymbols.SymbolTable.Bool), new LiteralNode(false, RuntimeSymbols.SymbolTable.Bool));
        SetupMockAndAssertCorrect(ast, translationTable, visitor);
    }

    [Theory, AutoData]
    public void BinaryLogicalWithMismatchFirst_ReturnsException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new OrNode(new LiteralNode(1.4, RuntimeSymbols.SymbolTable.Float), new LiteralNode(true, RuntimeSymbols.SymbolTable.Bool));
        SetupMockAndAssertError(ast, translationTable, visitor);
    }

    [Theory, AutoData]
    public void BinaryLogicalWithMismatchSecond_ReturnsException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new OrNode(new LiteralNode(true, RuntimeSymbols.SymbolTable.Bool), new LiteralNode(1.4, RuntimeSymbols.SymbolTable.Float));
        SetupMockAndAssertError(ast, translationTable, visitor);
    }

    [Theory, AutoData]
    public void BinaryLogicalWithMismatchBoth_ReturnsException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new OrNode(new LiteralNode(1.4, RuntimeSymbols.SymbolTable.Float), new LiteralNode(1.4, RuntimeSymbols.SymbolTable.Float));
        SetupMockAndAssertError(ast, translationTable, visitor);
    }

    [Theory, AutoData]
    public void BinaryComparisionDifferentType_ReturnsException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new LessNode(new LiteralNode(1, RuntimeSymbols.SymbolTable.Int), new LiteralNode(1.4, RuntimeSymbols.SymbolTable.Float));
        SetupMockAndAssertCorrect(ast, translationTable, visitor);
    }

    [Theory, AutoData]
    public void BinaryComparisionSameType_ReturnsNoException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new GreaterNode(new LiteralNode(1, RuntimeSymbols.SymbolTable.Int), new LiteralNode(0, RuntimeSymbols.SymbolTable.Int));
        SetupMockAndAssertCorrect(ast, translationTable, visitor);
    }

    [Theory, AutoData]
    public void BinaryArithmeticalDifferentType_ReturnsException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new AddNode(new LiteralNode(2, RuntimeSymbols.SymbolTable.Int), new LiteralNode(true, RuntimeSymbols.SymbolTable.Bool));
        SetupMockAndAssertError(ast, translationTable, visitor);
    }

    [Theory, AutoData]
    public void BinaryArithmeticalSameType_ReturnsNoException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new DivNode(new LiteralNode(1, RuntimeSymbols.SymbolTable.Int), new LiteralNode(0, RuntimeSymbols.SymbolTable.Int));
        SetupMockAndAssertCorrect(ast, translationTable, visitor);
    }

    private void SetupMockAndAssertCorrect(NodeBase ast, Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var filePosition = new FilePosition();
        translationTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        var context = new PreCreationContext(translationTable.Object, SymbolTableInitHelper.CreateDefaultTables());
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldBeEmpty(); 
    }
    
    private void SetupMockAndAssertError(NodeBase ast, Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var filePosition = new FilePosition();
        translationTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        translationTable.Setup(x => x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>()))
            .Returns<NodeBase, PlampExceptionRecord>((_, b) => new PlampException(b, default));

        var context = new PreCreationContext(translationTable.Object, SymbolTableInitHelper.CreateDefaultTables());
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.CannotApplyOperator().Code);
    }
}