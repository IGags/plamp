using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using plamp.Intrinsics;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class ConditionTypeInferenceTests
{
    [Theory, AutoData]
    public void ConditionWithCorrectPredicateType_ReturnNoException(
        [Frozen] Mock<ITranslationTable> translationTable,
        TypeInferenceWeaver visitor)
    {
        var ast = new ConditionNode(
            new LiteralNode(true, RuntimeSymbols.GetSymbolTable.MakeLogical()),
            new BodyNode([]), null);
        SetupMocksAndAssertCorrect(ast, translationTable, new SymbolTable("mod", []), visitor);
    }

    [Theory, AutoData]
    public void ConditionWithIncorrectPredicateType_ReturnsException(
        [Frozen] Mock<ITranslationTable> translationTable,
        TypeInferenceWeaver visitor)
    {
        var ast = new ConditionNode(
            new LiteralNode(1, RuntimeSymbols.GetSymbolTable.MakeInt()),
            new BodyNode([]), new BodyNode([]));
        
        SetupExceptionGenerationMock(translationTable);
        var context = new PreCreationContext(translationTable.Object, new SymbolTable("mod", []));
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.PredicateMustBeBooleanType().Code));
    }
    
    private void SetupMocksAndAssertCorrect(NodeBase ast, Mock<ITranslationTable> translationTable, SymbolTable symbolTable, TypeInferenceWeaver visitor)
    {
        var filePosition = new FilePosition();
        translationTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        var context = new PreCreationContext(translationTable.Object, symbolTable);
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldBeEmpty();
    }
    
    private void SetupExceptionGenerationMock(Mock<ITranslationTable> symbolTable)
    {
        var filePosition = new FilePosition();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        symbolTable.Setup(x => x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>()))
            .Returns<NodeBase, PlampExceptionRecord>((_, b) => new PlampException(b, default));
    }
}