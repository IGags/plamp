using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.Alternative.SymbolsImpl;
using Shouldly;
using Xunit;
using TypeBuilder = plamp.Alternative.SymbolsBuildingImpl.TypeBuilder;

namespace plamp.Alternative.Tests.SymbolsBuildingImpl;

public class GenericTypeBuilderTests
{
    /// <summary>
    /// Нельзя создать реализацию дженерик типа не из объявления дженерик типа.
    /// </summary>
    [Fact]
    public void CreateFromNonGenericDefinition_Throws()
    {
        var definition = new TypeBuilder("Pair", "test");

        Should.Throw<InvalidOperationException>(() => new GenericTypeBuilder(definition, [Builtins.Int]));
    }

    /// <summary>
    /// Аргумент закрытого дженерик типа не может быть объявлением дженерик типа.
    /// </summary>
    [Fact]
    public void CreateWithGenericDefinitionArgument_Throws()
    {
        var definition = new TypeBuilder("Pair", [new GenericParameterBuilder("T", "test")], "test");
        var genericArgument = new TypeBuilder("Box", [new GenericParameterBuilder("T", "test")], "test");

        Should.Throw<InvalidOperationException>(() => new GenericTypeBuilder(definition, [genericArgument]));
    }

    /// <summary>
    /// Число аргументов должно совпадать с числом параметров объявления.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void CreateWithMismatchArgumentCount_Throws(int argumentsCount)
    {
        var definition = new TypeBuilder("Box", [new GenericParameterBuilder("T", "test")], "test");
        var arguments = Enumerable.Repeat(Builtins.Int, argumentsCount).ToArray();

        Should.Throw<InvalidOperationException>(() => new GenericTypeBuilder(definition, arguments));
    }

    /// <summary>
    /// Реализация дженерик типа возвращает корректные имя, модуль и флаги типа.
    /// </summary>
    [Fact]
    public void GenericTypeOverall_Correct()
    {
        var definition = new TypeBuilder("Pair", [
            new GenericParameterBuilder("TKey", "test"),
            new GenericParameterBuilder("TValue", "test")
        ], "test");

        var impl = new GenericTypeBuilder(definition, [Builtins.String, Builtins.Int]);

        impl.Name.ShouldBe("Pair[string, int]");
        impl.DefinitionName.ShouldBe("Pair");
        impl.ModuleName.ShouldBe("test");
        impl.IsArrayType.ShouldBeFalse();
        impl.IsGenericType.ShouldBeTrue();
        impl.IsGenericTypeDefinition.ShouldBeFalse();
        impl.IsGenericTypeParameter.ShouldBeFalse();
    }

    /// <summary>
    /// Реализация дженерик типа возвращает исходное объявление и переданные аргументы.
    /// </summary>
    [Fact]
    public void GenericInfoCollections_Correct()
    {
        var definition = new TypeBuilder("Pair", [
            new GenericParameterBuilder("TKey", "test"),
            new GenericParameterBuilder("TValue", "test")
        ], "test");

        var impl = new GenericTypeBuilder(definition, [Builtins.String, Builtins.Int]);

        impl.GetGenericTypeDefinition().ShouldBe(definition);
        impl.GetGenericArguments().ShouldBe([Builtins.String, Builtins.Int]);
        impl.GetGenericParameters().ShouldBeEmpty();
    }

    /// <summary>
    /// Закрытый дженерик тип можно превратить в runtime .net тип.
    /// </summary>
    [Fact]
    public void AsType_ReturnsConstructedRuntimeType()
    {
        var definition = TypeInfo.FromType(typeof(Dictionary<,>), "test");
        var impl = new GenericTypeBuilder(definition, [Builtins.String, Builtins.Int]);

        impl.AsType().ShouldBe(typeof(Dictionary<string, int>));
    }

    /// <summary>
    /// Реализация дженерик типа позволяет построить массив с собой в качестве элемента.
    /// </summary>
    [Fact]
    public void MakeArrayType_ReturnsArrayType()
    {
        var definition = new TypeBuilder("Box", [new GenericParameterBuilder("T", "test")], "test");
        var impl = new GenericTypeBuilder(definition, [Builtins.Int]);

        var arrayType = impl.MakeArrayType();

        arrayType.ShouldBeAssignableTo<ArrayTypeBuilder>();
        arrayType.IsArrayType.ShouldBeTrue();
        arrayType.ElementType().ShouldBe(impl);
    }

    /// <summary>
    /// Закрытый дженерик тип не является массивом и не может быть повторно закрыт как объявление.
    /// </summary>
    [Fact]
    public void NonGenericDefinitionOperations_ReturnNull()
    {
        var definition = new TypeBuilder("Box", [new GenericParameterBuilder("T", "test")], "test");
        var impl = new GenericTypeBuilder(definition, [Builtins.Int]);

        impl.ElementType().ShouldBeNull();
        impl.MakeGenericType([Builtins.String]).ShouldBeNull();
    }

    /// <summary>
    /// При создании реализации generic-параметры в полях заменяются переданными аргументами.
    /// </summary>
    [Fact]
    public void FieldsReplaceGenericParameters_Correct()
    {
        var genericParam = new GenericParameterBuilder("T", "test");
        var definition = new TypeBuilder("Box", [genericParam], "test");
        var genericFieldNode = new FieldDefNode(
            new TypeNode(new TypeNameNode("T")) { TypeInfo = genericParam },
            new FieldNameNode("Value"));
        var simpleFieldNode = new FieldDefNode(
            new TypeNode(new TypeNameNode("int")) { TypeInfo = Builtins.Int },
            new FieldNameNode("Count"));
        definition.AddField(genericFieldNode);
        definition.AddField(simpleFieldNode);

        var impl = new GenericTypeBuilder(definition, [Builtins.String]);

        var fields = impl.Fields.OrderBy(x => x.Name).ToArray();
        fields.Length.ShouldBe(2);
        fields[0].Name.ShouldBe("Count");
        fields[0].FieldType.ShouldBe(Builtins.Int);
        fields[1].Name.ShouldBe("Value");
        fields[1].FieldType.ShouldBe(Builtins.String);
    }

    /// <summary>
    /// Подстановка generic-параметров работает рекурсивно внутри массивов и вложенных generic-типов.
    /// </summary>
    [Fact]
    public void FieldsReplaceGenericParametersRecursively_Correct()
    {
        var holderKeyParam = new GenericParameterBuilder("TKey", "test");
        var holderValueParam = new GenericParameterBuilder("TValue", "test");
        var mapKeyParam = new GenericParameterBuilder("TKey", "test");
        var mapValueParam = new GenericParameterBuilder("TValue", "test");
        var fieldKeyParam = new GenericParameterBuilder("TKey", "test");
        var fieldValueParam = new GenericParameterBuilder("TValue", "test");
        var mapDefinition = new TypeBuilder("Map", [
            mapKeyParam,
            mapValueParam
        ], "test");
        var fieldType = mapDefinition.MakeGenericType([fieldKeyParam, fieldValueParam]).ShouldNotBeNull().MakeArrayType();
        var definition = new TypeBuilder("Holder", [holderKeyParam, holderValueParam], "test");
        var fieldNode = new FieldDefNode(
            new TypeNode(new TypeNameNode("Map")) { TypeInfo = fieldType },
            new FieldNameNode("Items"));
        definition.AddField(fieldNode);

        var impl = new GenericTypeBuilder(definition, [Builtins.String, Builtins.Int]);

        var field = impl.Fields.ShouldHaveSingleItem();
        var expectedType = mapDefinition.MakeGenericType([Builtins.String, Builtins.Int]).ShouldNotBeNull().MakeArrayType();
        field.Name.ShouldBe("Items");
        field.FieldType.ShouldBe(expectedType);
    }

    /// <summary>
    /// Две реализации одного объявления с одинаковыми аргументами считаются равными и имеют одинаковый hash.
    /// </summary>
    [Fact]
    public void EqualityWithSameDefinitionAndArguments_Correct()
    {
        var definition = new TypeBuilder("Box", [new GenericParameterBuilder("T", "test")], "test");
        var impl = new GenericTypeBuilder(definition, [Builtins.Int]);
        var sameImpl = new GenericTypeBuilder(definition, [Builtins.Int]);

        impl.ShouldBe(sameImpl);
        impl.GetHashCode().ShouldBe(sameImpl.GetHashCode());
    }

    /// <summary>
    /// Реализации с разными аргументами или разными объявлениями не считаются равными.
    /// </summary>
    [Fact]
    public void EqualityWithDifferentDefinitionOrArguments_ReturnsFalse()
    {
        var definition = new TypeBuilder("Box", [new GenericParameterBuilder("T", "test")], "test");
        var otherDefinition = new TypeBuilder("OtherBox", [new GenericParameterBuilder("T", "test")], "test");
        var impl = new GenericTypeBuilder(definition, [Builtins.Int]);
        var implWithOtherArgument = new GenericTypeBuilder(definition, [Builtins.String]);
        var implWithOtherDefinition = new GenericTypeBuilder(otherDefinition, [Builtins.Int]);

        impl.Equals(implWithOtherArgument).ShouldBeFalse();
        impl.Equals(implWithOtherDefinition).ShouldBeFalse();
    }
}
