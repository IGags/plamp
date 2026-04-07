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
    public void UnresolvedFuncReturnType_ReturnsUnexpectedType([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var ast = new FuncNode(
            new TypeNode(new TypeNameNode("abc")),
            new FuncNameNode("aaa"),
            [],
            [],
            new BodyNode([new ReturnNode(null)])); 
        SetupExceptionMock(translationTable);
        var context = new PreCreationContext(translationTable.Object, SymbolTableInitHelper.CreateDefaultTables());
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.TypeIsNotFound("aaa").Code));
    }

    [Theory, AutoData]
    public void ReturnSameTypeAsFunc_ReturnNoException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };
        var ast = new FuncNode(
            returnType, new FuncNameNode("aaa"), [], [],
            new BodyNode([new ReturnNode(new LiteralNode(1, Builtins.Int))]));
        SetupMockAndAssertCorrect(ast, translationTable, visitor);
    }
    
    [Theory, AutoData]
    public void VoidFuncReturnNull_ReturnNoException([Frozen] Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new TypeNameNode("void"))
        {
            TypeInfo = Builtins.Void
        };
        var ast = new FuncNode(
            returnType, new FuncNameNode("aaa"), [], [],
            new BodyNode([new ReturnNode(null)]));
        SetupMockAndAssertCorrect(ast, translationTable, visitor);
    }

    [Theory, AutoData]
    public void ReturnDifferentTypeFromFunc_ReturnException(
        [Frozen] Mock<ITranslationTable> translationTable,
        TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };
        var ast = new FuncNode(
            returnType, new FuncNameNode("aaa"), [], [],
            new BodyNode([new ReturnNode(new LiteralNode(1d, Builtins.Double))]));
        SetupExceptionMock(translationTable);
        var context = new PreCreationContext(translationTable.Object, SymbolTableInitHelper.CreateDefaultTables());
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.ReturnTypeMismatch().Code));
    }

    [Theory, AutoData]
    public void FuncVoidNodeReturnValue_ReturnException(
        [Frozen] Mock<ITranslationTable> translationTable,
        TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new TypeNameNode("void"))
        {
            TypeInfo = Builtins.Void
        };
        var ast = new FuncNode(
            returnType, new FuncNameNode("aaa"), [], [],
            new BodyNode([new ReturnNode(new LiteralNode(1, Builtins.Int))]));
        SetupExceptionMock(translationTable);
        var context = new PreCreationContext(translationTable.Object, SymbolTableInitHelper.CreateDefaultTables());
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotReturnValue().Code));
    }
    
    [Theory, AutoData]
    public void FuncHasReturnTypeNodeReturnNull_ReturnException(
        [Frozen] Mock<ITranslationTable> translationTable,
        TypeInferenceWeaver visitor)
    {
        var returnType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };
        var ast = new FuncNode(
            returnType, new FuncNameNode("aaa"), [], [],
            new BodyNode([new ReturnNode(null)]));
        SetupExceptionMock(translationTable);
        var context = new PreCreationContext(translationTable.Object, SymbolTableInitHelper.CreateDefaultTables());
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
        var preCreation = new PreCreationContext(context.TranslationTable, SymbolTableInitHelper.CreateDefaultTables());
        var weaveResult = visitor.WeaveDiffs(expression!, preCreation);
        expression
            .ShouldBeOfType<FuncNode>()
            .Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<ReturnNode>()
            .ReturnValue.ShouldBeOfType<CastNode>()
            .ShouldSatisfyAllConditions(
                x => x.FromType.ShouldBe(Builtins.Int),
                x => x.ToType.ShouldBeOfType<TypeNode>().TypeInfo.ShouldBe(Builtins.Long));
        weaveResult.Exceptions.ShouldBeEmpty();
    }

    private void SetupExceptionMock(Mock<ITranslationTable> translationTable)
    {
        var filePosition = new FilePosition();
        translationTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        translationTable.Setup(x => x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>()))
            .Returns<NodeBase, PlampExceptionRecord>((_, b) => new PlampException(b, default));
    }
    
    private void SetupMockAndAssertCorrect(NodeBase ast, Mock<ITranslationTable> translationTable, TypeInferenceWeaver visitor)
    {
        var filePosition = new FilePosition();
        translationTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        var context = new PreCreationContext(translationTable.Object, SymbolTableInitHelper.CreateDefaultTables());
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldBeEmpty(); 
    }
}