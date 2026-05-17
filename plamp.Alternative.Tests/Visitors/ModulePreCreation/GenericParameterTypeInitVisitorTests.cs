using System.Collections.Generic;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference.Util;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.GenericParameterTypeInitialization;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation;

public class GenericParameterTypeInitVisitorTests
{
    /// <summary>
    /// Объект типа generic-параметра создать нельзя
    /// </summary>
    [Fact]
    public void InitGenericParameterType_ReturnsException()
    {
        var ast = CreateGenericParameterTypeInit();
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldHaveSingleItem().Code
            .ShouldBe(PlampExceptionInfo.CannotCreateGenericParameterType().Code);
    }

    /// <summary>
    /// Объект типа generic-параметра нельзя создать внутри тела функции
    /// </summary>
    [Fact]
    public void InitGenericParameterTypeInsideFuncBody_ReturnsException()
    {
        var ast = new FuncNode(
            CreateTypeNode("void", Builtins.Void),
            new FuncNameNode("mock"),
            [],
            [],
            new BodyNode([CreateGenericParameterTypeInit()]));

        var context = SetupAndAct(ast);

        context.Exceptions.ShouldHaveSingleItem().Code
            .ShouldBe(PlampExceptionInfo.CannotCreateGenericParameterType().Code);
    }

    /// <summary>
    /// Некорректная инициализация generic-параметра не обходит вложенные поля
    /// </summary>
    [Fact]
    public void InitGenericParameterTypeWithFieldInitializers_ReturnsSingleException()
    {
        var ast = CreateGenericParameterTypeInit(
        [
            new InitFieldNode(new FieldNameNode("value"), CreateGenericParameterTypeInit())
        ]);

        var context = SetupAndAct(ast);

        context.Exceptions.ShouldHaveSingleItem().Code
            .ShouldBe(PlampExceptionInfo.CannotCreateGenericParameterType().Code);
    }

    /// <summary>
    /// Вложенный объект типа generic-параметра внутри обычного init тоже запрещён
    /// </summary>
    [Fact]
    public void InitGenericParameterTypeNestedIntoNonGenericType_ReturnsException()
    {
        var ast = new InitTypeNode(
            CreateTypeNode("User", new TypeBuilder("User", "test")),
            [
                new InitFieldNode(new FieldNameNode("value"), CreateGenericParameterTypeInit())
            ]);

        var context = SetupAndAct(ast);

        context.Exceptions.ShouldHaveSingleItem().Code
            .ShouldBe(PlampExceptionInfo.CannotCreateGenericParameterType().Code);
    }

    /// <summary>
    /// Объект обычного типа создать можно
    /// </summary>
    [Fact]
    public void InitNonGenericType_Correct()
    {
        var ast = new InitTypeNode(CreateTypeNode("User", new TypeBuilder("User", "test")), []);
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Этот visitor не запрещает инициализацию builtin-типа
    /// </summary>
    [Fact]
    public void InitBuiltinType_HasNoGenericParameterInitException()
    {
        var ast = new InitTypeNode(CreateTypeNode("int", Builtins.Int), []);
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Объект закрытого generic-типа создать можно, даже если его аргументом является generic-параметр
    /// </summary>
    [Fact]
    public void InitGenericTypeWithGenericParameterArgument_Correct()
    {
        var genericParameter = new GenericParameterBuilder("T", "test");
        var genericDefinition = new TypeBuilder("Box", [genericParameter], "test");
        var genericType = genericDefinition.MakeGenericType([genericParameter]).ShouldNotBeNull();

        var ast = new InitTypeNode(CreateTypeNode("Box", genericType), []);
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Инициализация типа массива не считается инициализацией generic-параметра
    /// </summary>
    [Fact]
    public void InitArrayTypeOfGenericParameter_HasNoGenericParameterInitException()
    {
        var genericParameter = new GenericParameterBuilder("T", "test");
        var itemType = CreateTypeNode("T", genericParameter.MakeArrayType());
        itemType.ArrayDefinitions.Add(new ArrayTypeSpecificationNode());

        var ast = new InitTypeNode(itemType, []);
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Узел без выведенного типа пропускается
    /// </summary>
    [Fact]
    public void InitTypeWithoutTypeInfo_Correct()
    {
        var ast = new InitTypeNode(new TypeNode(new TypeNameNode("T")), []);
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    private static InitTypeNode CreateGenericParameterTypeInit()
    {
        return CreateGenericParameterTypeInit([]);
    }

    private static InitTypeNode CreateGenericParameterTypeInit(List<InitFieldNode> fieldInitializers)
    {
        var genericParameter = new GenericParameterBuilder("T", "test");
        return new InitTypeNode(CreateTypeNode("T", genericParameter), fieldInitializers);
    }

    private static TypeNode CreateTypeNode(string typeName, ITypeInfo typeInfo)
    {
        return new TypeNode(new TypeNameNode(typeName)) { TypeInfo = typeInfo };
    }

    private static PreCreationContext SetupAndAct(NodeBase ast)
    {
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();
        return new GenericParameterTypeInitVisitor().Validate(ast, context);
    }
}
