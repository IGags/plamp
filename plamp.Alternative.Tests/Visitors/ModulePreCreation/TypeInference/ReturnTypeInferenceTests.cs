using AutoFixture;
using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
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

public class ReturnTypeInferenceTests
{
    [Theory, AutoData]
    public void UnresolvedFuncReturnType_ReturnsUnexpectedType([Frozen] Mock<ISymbolTable> symbolTable,TypeInferenceWeaver visitor)
    {
        var ast = new FuncNode(
            new TypeNode(new TypeNameNode("abc")),
            new FuncNameNode("aaa"),
            [],
            new BodyNode([new ReturnNode(null)])); 
        SetupExceptionMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.TypesIsNotSupported().Code));
    }

    [Theory, AutoData]
    public void ReturnSameTypeAsFunc_ReturnNoException([Frozen] Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new TypeNameNode("int"));
        returnType.SetType(typeof(int));
        var ast = new FuncNode(
            returnType, new FuncNameNode("aaa"), [],
            new BodyNode([new ReturnNode(new LiteralNode(1, typeof(int)))]));
        SetupMockAndAssertCorrect(ast, symbolTable, visitor);
    }
    
    [Theory, AutoData]
    public void VoidFuncReturnNull_ReturnNoException([Frozen] Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new TypeNameNode("void"));
        returnType.SetType(typeof(void));
        var ast = new FuncNode(
            returnType, new FuncNameNode("aaa"), [],
            new BodyNode([new ReturnNode(null)]));
        SetupMockAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void ReturnDifferentTypeFromFunc_ReturnException(
        [Frozen] Mock<ISymbolTable> symbolTable,
        TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new TypeNameNode("int"));
        returnType.SetType(typeof(int));
        var ast = new FuncNode(
            returnType, new FuncNameNode("aaa"), [],
            new BodyNode([new ReturnNode(new LiteralNode(1d, typeof(double)))]));
        SetupExceptionMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.ReturnTypeMismatch().Code));
    }

    [Theory, AutoData]
    public void FuncVoidNodeReturnValue_ReturnException(
        [Frozen] Mock<ISymbolTable> symbolTable,
        TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new TypeNameNode("void"));
        returnType.SetType(typeof(void));
        var ast = new FuncNode(
            returnType, new FuncNameNode("aaa"), [],
            new BodyNode([new ReturnNode(new LiteralNode(1, typeof(int)))]));
        SetupExceptionMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotReturnValue().Code));
    }
    
    [Theory, AutoData]
    public void FuncHasReturnTypeNodeReturnNull_ReturnException(
        [Frozen] Mock<ISymbolTable> symbolTable,
        TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new TypeNameNode("int"));
        returnType.SetType(typeof(int));
        var ast = new FuncNode(
            returnType, new FuncNameNode("aaa"), [],
            new BodyNode([new ReturnNode(null)]));
        SetupExceptionMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.ReturnValueIsMissing().Code));
    }

    [Fact]
    public void ReturnIntFromLongFunction_Correct()
    {
        const string code = "fn ret() long { return 1i; }";
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseTopLevel(context, out var expression);
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.SymbolTable);
        var weaveResult = visitor.WeaveDiffs(expression!, preCreation);
        expression
            .ShouldBeOfType<FuncNode>()
            .Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<ReturnNode>()
            .ReturnValue.ShouldBeOfType<CastNode>()
            .ShouldSatisfyAllConditions(
                x => x.FromType.ShouldBe(typeof(int)),
                x => x.ToType.ShouldBeOfType<TypeNode>().Symbol.ShouldBe(typeof(long)));
        weaveResult.Exceptions.ShouldBeEmpty();
    }

    private void SetupExceptionMock(Mock<ISymbolTable> symbolTable)
    {
        var filePosition = new FilePosition();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        symbolTable.Setup(x => x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>()))
            .Returns<NodeBase, PlampExceptionRecord>((_, b) => new PlampException(b, default));
    }
    
    private void SetupMockAndAssertCorrect(NodeBase ast, Mock<ISymbolTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var filePosition = new FilePosition();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldBeEmpty(); 
    }
}