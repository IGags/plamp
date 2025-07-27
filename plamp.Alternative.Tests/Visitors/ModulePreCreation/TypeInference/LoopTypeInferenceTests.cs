using System.Collections.Generic;
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
    public void WhileLoopWithCorrectCondition_ReturnNoException([Frozen] Mock<ISymbolTable> symbolTable,
        string fileName, TypeInferenceWeaver visitor)
    {
        var ast = new WhileNode(
            new LiteralNode(true, typeof(bool)),
            new BodyNode([]));
        SetupMocksAndAssertCorrect(ast, symbolTable, fileName, visitor);
    }
    
    private void SetupMocksAndAssertCorrect(NodeBase ast, Mock<ISymbolTable> symbolTable, string fileName, TypeInferenceWeaver visitor)
    {
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        var context = new PreCreationContext(fileName, symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldBeEmpty();
    }
}