using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.GenericVariableInit;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation;

public class GenericVariableInitValidatorTests
{
    /// <summary>
    /// Переменную типа generic-параметра нельзя объявить без явной инициализации
    /// </summary>
    [Fact]
    public void DefineGenericParameterVariableInBody_ReturnsException()
    {
        var ast = new BodyNode([CreateGenericParameterVariableDefinition("value")]);
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldHaveSingleItem().Code
            .ShouldBe(PlampExceptionInfo.CannotCreateGenericParameterType().Code);
    }

    /// <summary>
    /// Несколько переменных типа generic-параметра без явной инициализации дают ошибку на каждое объявление
    /// </summary>
    [Fact]
    public void DefineSeveralGenericParameterVariablesInBody_ReturnsExceptions()
    {
        var ast = new BodyNode(
        [
            CreateGenericParameterVariableDefinition("first"),
            CreateGenericParameterVariableDefinition("second")
        ]);

        var context = SetupAndAct(ast);

        context.Exceptions.Count.ShouldBe(2);
        context.Exceptions.ShouldAllBe(x => x.Code == PlampExceptionInfo.CannotCreateGenericParameterType().Code);
    }

    /// <summary>
    /// Переменную типа generic-параметра нельзя объявить без явной инициализации внутри тела функции
    /// </summary>
    [Fact]
    public void DefineGenericParameterVariableInsideFuncBody_ReturnsException()
    {
        var ast = new FuncNode(
            CreateTypeNode("void", Builtins.Void),
            new FuncNameNode("mock"),
            [],
            [],
            new BodyNode([CreateGenericParameterVariableDefinition("value")]));

        var context = SetupAndAct(ast);

        context.Exceptions.ShouldHaveSingleItem().Code
            .ShouldBe(PlampExceptionInfo.CannotCreateGenericParameterType().Code);
    }

    /// <summary>
    /// Переменную типа generic-параметра можно объявить с явной инициализацией
    /// </summary>
    [Fact]
    public void DefineGenericParameterVariableInAssign_Correct()
    {
        var ast = new BodyNode(
        [
            new AssignNode(
                [CreateGenericParameterVariableDefinition("value")],
                [new LiteralNode(1, Builtins.Int)])
        ]);

        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Объявление переменной обычного типа без явной инициализации не запрещается этим validator-ом
    /// </summary>
    [Fact]
    public void DefineNonGenericVariableInBody_Correct()
    {
        var ast = new BodyNode([CreateVariableDefinition("value", Builtins.Int)]);
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Объявление переменной закрытого generic-типа без явной инициализации не считается объявлением generic-параметра
    /// </summary>
    [Fact]
    public void DefineGenericTypeVariableInBody_Correct()
    {
        var genericParameter = new GenericParameterBuilder("T", "test");
        var genericDefinition = new TypeBuilder("Box", [genericParameter], "test");
        var genericType = genericDefinition.MakeGenericType([Builtins.Int]).ShouldNotBeNull();

        var ast = new BodyNode([CreateVariableDefinition("value", genericType)]);
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Объявление переменной типа массива generic-параметра без явной инициализации не считается объявлением generic-параметра
    /// </summary>
    [Fact]
    public void DefineGenericParameterArrayVariableInBody_Correct()
    {
        var genericParameter = new GenericParameterBuilder("T", "test");
        var type = CreateTypeNode("T", genericParameter.MakeArrayType());
        type.ArrayDefinitions.Add(new ArrayTypeSpecificationNode());
        var ast = new BodyNode([new VariableDefinitionNode(type, new VariableNameNode("value"))]);

        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Объявление переменной без выведенного типа пропускается
    /// </summary>
    [Fact]
    public void DefineVariableWithoutTypeInfo_Correct()
    {
        var ast = new BodyNode([new VariableDefinitionNode(new TypeNode(new TypeNameNode("T")), new VariableNameNode("value"))]);
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Узел объявления generic-переменной без родителя-body не обрабатывается
    /// </summary>
    [Fact]
    public void DefineGenericParameterVariableOutsideBody_Correct()
    {
        var ast = CreateGenericParameterVariableDefinition("value");
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    private static VariableDefinitionNode CreateGenericParameterVariableDefinition(string variableName)
    {
        var genericParameter = new GenericParameterBuilder("T", "test");
        return CreateVariableDefinition(variableName, genericParameter);
    }

    private static VariableDefinitionNode CreateVariableDefinition(string variableName, ITypeInfo typeInfo)
    {
        return new VariableDefinitionNode(CreateTypeNode(typeInfo.Name, typeInfo), new VariableNameNode(variableName));
    }

    private static TypeNode CreateTypeNode(string typeName, ITypeInfo typeInfo)
    {
        return new TypeNode(new TypeNameNode(typeName)) { TypeInfo = typeInfo };
    }

    private static PreCreationContext SetupAndAct(NodeBase ast)
    {
        var translationTable = new Mock<ITranslationTable>();
        translationTable
            .Setup(x => x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>()))
            .Returns<NodeBase, PlampExceptionRecord>((_, record) => new PlampException(record, default));
        var context = new PreCreationContext(translationTable.Object, SymbolTableInitHelper.CreateDefaultTables());
        return new GenericVariableInitValidator().Validate(ast, context);
    }
}
