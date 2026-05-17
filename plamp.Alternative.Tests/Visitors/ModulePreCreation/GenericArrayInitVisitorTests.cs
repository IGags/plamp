using System.Collections.Generic;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference.Util;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.GenericArrayInitialization;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation;

public class GenericArrayInitVisitorTests
{
    public static IEnumerable<object[]> NonZeroIntConvertibleLiteralProvider()
    {
        yield return [(byte)1, Builtins.Byte];
        yield return [(sbyte)1, Builtins.Sbyte];
        yield return [(short)1, Builtins.Short];
        yield return [(ushort)1, Builtins.Ushort];
        yield return [1, Builtins.Int];
        yield return [1u, Builtins.Uint];
    }

    public static IEnumerable<object[]> ZeroIntConvertibleLiteralProvider()
    {
        yield return [(byte)0, Builtins.Byte];
        yield return [(sbyte)0, Builtins.Sbyte];
        yield return [(short)0, Builtins.Short];
        yield return [(ushort)0, Builtins.Ushort];
        yield return [0, Builtins.Int];
        yield return [0u, Builtins.Uint];
    }

    /// <summary>
    /// Непустой массив от generic-параметра нельзя создать по литералу длины
    /// </summary>
    [Theory]
    [MemberData(nameof(NonZeroIntConvertibleLiteralProvider))]
    public void InitArrayOfGenericParameterWithNonZeroLiteral_ReturnsException(object length, ITypeInfo lengthType)
    {
        var ast = CreateGenericParameterArrayInit(new LiteralNode(length, lengthType));
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldHaveSingleItem().Code
            .ShouldBe(PlampExceptionInfo.CannotCreateNonEmptyArrayOfGenericParameter().Code);
    }

    /// <summary>
    /// Непустой массив от generic-параметра нельзя создать по приведённому литералу длины
    /// </summary>
    [Fact]
    public void InitArrayOfGenericParameterWithNonZeroCastLiteral_ReturnsException()
    {
        var ast = CreateGenericParameterArrayInit(CreateCastToInt(new LiteralNode((byte)1, Builtins.Byte)));
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldHaveSingleItem().Code
            .ShouldBe(PlampExceptionInfo.CannotCreateNonEmptyArrayOfGenericParameter().Code);
    }

    /// <summary>
    /// Массив от generic-параметра с неизвестной на этапе проверки длиной запрещён
    /// </summary>
    [Fact]
    public void InitArrayOfGenericParameterWithDynamicLength_ReturnsException()
    {
        var ast = CreateGenericParameterArrayInit(new MemberNode("length"));
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldHaveSingleItem().Code
            .ShouldBe(PlampExceptionInfo.CannotCreateNonEmptyArrayOfGenericParameter().Code);
    }

    /// <summary>
    /// Пустой массив от generic-параметра создать можно
    /// </summary>
    [Theory]
    [MemberData(nameof(ZeroIntConvertibleLiteralProvider))]
    public void InitArrayOfGenericParameterWithZeroLiteral_Correct(object length, ITypeInfo lengthType)
    {
        var ast = CreateGenericParameterArrayInit(new LiteralNode(length, lengthType));
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Пустой массив от generic-параметра с приведённой длиной создать можно
    /// </summary>
    [Fact]
    public void InitArrayOfGenericParameterWithZeroCastLiteral_Correct()
    {
        var ast = CreateGenericParameterArrayInit(CreateCastToInt(new LiteralNode((byte)0, Builtins.Byte)));
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Непустой массив от обычного типа создать можно
    /// </summary>
    [Fact]
    public void InitArrayOfNonGenericTypeWithNonZeroLength_Correct()
    {
        var ast = new InitArrayNode(CreateTypeNode("int", Builtins.Int), new LiteralNode(1, Builtins.Int));
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Непустой массив от типа массива generic-параметра создать можно
    /// </summary>
    [Fact]
    public void InitArrayOfGenericParameterArrayTypeWithNonZeroLength_Correct()
    {
        var genericParameter = new GenericParameterBuilder("T", "test");
        var itemType = CreateTypeNode("T", genericParameter.MakeArrayType());
        itemType.ArrayDefinitions.Add(new ArrayTypeSpecificationNode());
        var ast = new InitArrayNode(itemType, new LiteralNode(1, Builtins.Int));
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Узел без выведенного типа элемента массива пропускается
    /// </summary>
    [Fact]
    public void InitArrayWithoutItemTypeInfo_Correct()
    {
        var ast = new InitArrayNode(new TypeNode(new TypeNameNode("T")), new LiteralNode(1, Builtins.Int));
        var context = SetupAndAct(ast);

        context.Exceptions.ShouldBeEmpty();
    }

    private static InitArrayNode CreateGenericParameterArrayInit(NodeBase length)
    {
        var genericParameter = new GenericParameterBuilder("T", "test");
        return new InitArrayNode(CreateTypeNode("T", genericParameter), length);
    }

    private static TypeNode CreateTypeNode(string typeName, ITypeInfo typeInfo)
    {
        return new TypeNode(new TypeNameNode(typeName)) { TypeInfo = typeInfo };
    }

    private static CastNode CreateCastToInt(LiteralNode literal)
    {
        return new CastNode(CreateTypeNode("int", Builtins.Int), literal) { FromType = literal.Type };
    }

    private static PreCreationContext SetupAndAct(NodeBase ast)
    {
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization() } };
        var context = fixture.Create<PreCreationContext>();
        return new GenericArrayInitVisitor().Validate(ast, context);
    }
}
