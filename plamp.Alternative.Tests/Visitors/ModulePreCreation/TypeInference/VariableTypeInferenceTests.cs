using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class VariableTypeInferenceTests
{
    [Theory, AutoData]
    public void VariableDefinitionInference_ReturnNoExceptions([Frozen]Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void NotExistVariableInference_ReturnsVariableNotExistException([Frozen]Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var exceptionMember = new MemberNode("b");
        var ast = new BodyNode([
            new AssignNode([new MemberNode("a")], [exceptionMember])
        ]);
        
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        
        symbolTable.Verify(x => x.SetExceptionToNode(exceptionMember, It.IsAny<PlampExceptionRecord>()), Times.Once);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotFindMember().Code));
    }

    [Theory, AutoData]
    public void CreateAndUseVariableDefinition_ReturnNoException([Frozen]Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))]),
            new AssignNode([new MemberNode("b")], [new MemberNode("a")])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void CreateVariableAndAssignOtherType_InvalidOperationException([Frozen]Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var exceptionMember = new AssignNode([new MemberNode("a")], [new LiteralNode("123", typeof(string))]);
        var ast = new BodyNode([
            new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))]),
            exceptionMember
        ]);
        
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        
        symbolTable.Verify(x => x.SetExceptionToNode(exceptionMember, It.IsAny<PlampExceptionRecord>()), Times.Once);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotAssign().Code));
    }

    [Theory, AutoData]
    public void CreateVariableBeforeAndGetFromChildScope_ReturnsNoException([Frozen]Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))]),
            new BodyNode(
            [
                new AssignNode([new MemberNode("a")], [new LiteralNode(2, typeof(int))])
            ])
        ]);
        
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void CreateVariableAfterGetFromChildScope_ReturnsDuplicateDefinitionException([Frozen] Mock<ISymbolTable> symbolTable,
        TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new BodyNode(
            [
                new AssignNode([new MemberNode("a")], [new LiteralNode(2, typeof(int))])
            ]),
            new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))])
        ]);
        
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldSatisfyAllConditions(
                y => y.Count.ShouldBe(2),
                y => y[0].Code.ShouldBe(PlampExceptionInfo.DuplicateVariableDefinition().Code),
                y => y[1].Code.ShouldBe(PlampExceptionInfo.DuplicateVariableDefinition().Code))
        );
    }

    [Theory, AutoData]
    public void CreateVariableInOtherScopeStack_ReturnsNoException([Frozen] Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new BodyNode(
            [
                new AssignNode([new MemberNode("a")], [new LiteralNode(2, typeof(int))])
            ]),
            new BodyNode(
            [
                new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))])
            ])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void DefineVariableExplicitly_ReturnsNoException([Frozen] Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a"))
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void DefineVariableExplicitlyAndAssign_ReturnsNoException([Frozen] Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode(
                [new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a"))],
                [new LiteralNode(1, typeof(int))])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }


    [Theory, AutoData]
    public void DefineVariableExplicitlyTwice_ReturnsDuplicateDefinitionException(
        [Frozen] Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a")),
            new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a"))
        ]);
        
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldSatisfyAllConditions(
                y => y.Count.ShouldBe(2),
                y => y[0].Code.ShouldBe(PlampExceptionInfo.DuplicateVariableDefinition().Code),
                y => y[1].Code.ShouldBe(PlampExceptionInfo.DuplicateVariableDefinition().Code))
            );
    }

    [Theory, AutoData]
    public void DefineVariableAndAssignToOther_ReturnsNoException([Frozen] Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))]),
            new AssignNode([new MemberNode("b")], [new MemberNode("a")])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void AssignUndefined_ReturnsException([Frozen] Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("a")], [new MemberNode("b")])
        ]);
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotFindMember().Code));
    }

    [Theory, AutoData]
    public void AssignThemself_ReturnsException([Frozen] Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("a")], [new MemberNode("a")])
        ]);
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotFindMember().Code));
    }

    [Theory, AutoData]
    public void AssignEmptyDefinition_ReturnsNoException(
        [Frozen] Mock<ISymbolTable> symbolTable,
        TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a")),
            new AssignNode([new MemberNode("b")], [new MemberNode("a")])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void AssignThemselfAfterDefinition_ReturnsNoException([Frozen] Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a")),
            new AssignNode([new MemberNode("a")], [new MemberNode("a")])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
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