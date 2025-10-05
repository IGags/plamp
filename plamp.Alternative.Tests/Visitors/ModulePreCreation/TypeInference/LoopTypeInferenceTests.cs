using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class LoopTypeInferenceTests
{
    [Theory, AutoData]
    public void WhileLoopWithCorrectCondition_ReturnNoException(
        [Frozen] Mock<ISymbolTable> symbolTable,
        TypeInferenceWeaver visitor)
    {
        var ast = new WhileNode(
            new LiteralNode(true, typeof(bool)),
            new BodyNode([]));
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void WhileLoopWithIncorrectConditionType_ReturnsException(
        [Frozen] Mock<ISymbolTable> symbolTable,
        TypeInferenceWeaver visitor)
    {
        var ast = new WhileNode(
            new LiteralNode(1, typeof(int)),
            new BodyNode([]));
        
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.PredicateMustBeBooleanType().Code));
    }
    
    private void SetupMocksAndAssertCorrect(NodeBase ast, Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var filePosition = new FilePosition();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldBeEmpty();
    }
    
    private void SetupExceptionGenerationMock(Mock<ISymbolTable> symbolTable)
    {
        var filePosition = new FilePosition();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        symbolTable.Setup(x => x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>()))
            .Returns<NodeBase, PlampExceptionRecord>((_, b) => new PlampException(b, default));
    }
}