using System;
using System.Collections.Generic;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsBuildingImpl;
using Shouldly;
using Xunit;
using TypeBuilder = plamp.Alternative.SymbolsBuildingImpl.TypeBuilder;

namespace plamp.Alternative.Tests.SymbolsBuildingImpl;

public class GenericImplementationHelperTests
{
    private const string ModuleName = "test";

    /// <summary>
    /// Generic-параметр заменяется типом из маппинга.
    /// </summary>
    [Fact]
    public void ImplementType_GenericParameter_ReturnsMappedType()
    {
        var parameter = new GenericParameterBuilder("T", ModuleName);
        var mapping = new Dictionary<ITypeInfo, ITypeInfo>
        {
            [parameter] = Builtins.Int
        };

        var result = GenericImplementationHelper.ImplementType(parameter, mapping);

        result.ShouldBe(Builtins.Int);
    }

    /// <summary>
    /// Массив generic-параметров заменяется массивом типов реализации.
    /// </summary>
    [Fact]
    public void ImplementType_ArrayOfGenericParameter_ReturnsArrayOfMappedType()
    {
        var parameter = new GenericParameterBuilder("T", ModuleName);
        var openType = parameter.MakeArrayType();
        var mapping = new Dictionary<ITypeInfo, ITypeInfo>
        {
            [parameter] = Builtins.Int
        };

        var result = GenericImplementationHelper.ImplementType(openType, mapping);

        result.IsArrayType.ShouldBeTrue();
        result.ElementType().ShouldBe(Builtins.Int);
    }

    /// <summary>
    /// Generic-тип с generic-параметром в аргументе заменяет аргумент на тип реализации.
    /// </summary>
    [Fact]
    public void ImplementType_GenericTypeWithGenericParameterArgument_ReturnsClosedGenericType()
    {
        var parameter = new GenericParameterBuilder("T", ModuleName);
        var boxDefinition = new TypeBuilder("Box", [new GenericParameterBuilder("TBox", ModuleName)], ModuleName);
        var openType = boxDefinition.MakeGenericType([parameter]).ShouldNotBeNull();
        var mapping = new Dictionary<ITypeInfo, ITypeInfo>
        {
            [parameter] = Builtins.String
        };

        var result = GenericImplementationHelper.ImplementType(openType, mapping);

        result.ShouldBe(boxDefinition.MakeGenericType([Builtins.String]));
    }

    /// <summary>
    /// Generic-тип с вложенным generic-типом в аргументе реализуется рекурсивно.
    /// </summary>
    [Fact]
    public void ImplementType_GenericTypeWithGenericTypeParameterArgument_ReturnsClosedNestedGenericType()
    {
        var parameter = new GenericParameterBuilder("T", ModuleName);
        var boxDefinition = new TypeBuilder("Box", [new GenericParameterBuilder("TBox", ModuleName)], ModuleName);
        var holderDefinition = new TypeBuilder("Holder", [new GenericParameterBuilder("THolder", ModuleName)], ModuleName);
        var boxOfParameter = boxDefinition.MakeGenericType([parameter]).ShouldNotBeNull();
        var openType = holderDefinition.MakeGenericType([boxOfParameter]).ShouldNotBeNull();
        var mapping = new Dictionary<ITypeInfo, ITypeInfo>
        {
            [parameter] = Builtins.Int
        };

        var result = GenericImplementationHelper.ImplementType(openType, mapping);
        var expected = holderDefinition.MakeGenericType([
            boxDefinition.MakeGenericType([Builtins.Int]).ShouldNotBeNull()
        ]);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Generic-тип с массивом generic-параметров в аргументе реализует элемент массива.
    /// </summary>
    [Fact]
    public void ImplementType_GenericTypeWithArrayOfGenericParameterArgument_ReturnsClosedGenericType()
    {
        var parameter = new GenericParameterBuilder("T", ModuleName);
        var boxDefinition = new TypeBuilder("Box", [new GenericParameterBuilder("TBox", ModuleName)], ModuleName);
        var openType = boxDefinition.MakeGenericType([parameter.MakeArrayType()]).ShouldNotBeNull();
        var mapping = new Dictionary<ITypeInfo, ITypeInfo>
        {
            [parameter] = Builtins.String
        };

        var result = GenericImplementationHelper.ImplementType(openType, mapping);

        result.ShouldBe(boxDefinition.MakeGenericType([Builtins.String.MakeArrayType()]));
    }

    /// <summary>
    /// Массив generic-типа с generic-параметром реализует тип элемента рекурсивно.
    /// </summary>
    [Fact]
    public void ImplementType_ArrayOfGenericTypeWithGenericParameter_ReturnsArrayOfClosedGenericType()
    {
        var parameter = new GenericParameterBuilder("T", ModuleName);
        var boxDefinition = new TypeBuilder("Box", [new GenericParameterBuilder("TBox", ModuleName)], ModuleName);
        var boxOfParameter = boxDefinition.MakeGenericType([parameter]).ShouldNotBeNull();
        var openType = boxOfParameter.MakeArrayType();
        var mapping = new Dictionary<ITypeInfo, ITypeInfo>
        {
            [parameter] = Builtins.Int
        };

        var result = GenericImplementationHelper.ImplementType(openType, mapping);
        var expectedElement = boxDefinition.MakeGenericType([Builtins.Int]).ShouldNotBeNull();

        result.IsArrayType.ShouldBeTrue();
        result.ElementType().ShouldBe(expectedElement);
    }

    /// <summary>
    /// Generic-тип с несколькими параметрами заменяет каждый параметр своим типом реализации.
    /// </summary>
    [Fact]
    public void ImplementType_GenericTypeWithManyGenericParameters_ReturnsClosedGenericType()
    {
        var keyParameter = new GenericParameterBuilder("TKey", ModuleName);
        var valueParameter = new GenericParameterBuilder("TValue", ModuleName);
        var pairDefinition = new TypeBuilder("Pair", [
            new GenericParameterBuilder("TKey", ModuleName),
            new GenericParameterBuilder("TValue", ModuleName)
        ], ModuleName);
        var openType = pairDefinition.MakeGenericType([keyParameter, valueParameter]).ShouldNotBeNull();
        var mapping = new Dictionary<ITypeInfo, ITypeInfo>
        {
            [keyParameter] = Builtins.String,
            [valueParameter] = Builtins.Int
        };

        var result = GenericImplementationHelper.ImplementType(openType, mapping);

        result.ShouldBe(pairDefinition.MakeGenericType([Builtins.String, Builtins.Int]));
    }

    /// <summary>
    /// Неполный маппинг типов не позволяет реализовать generic-параметр.
    /// </summary>
    [Fact]
    public void ImplementType_MissingMappingValue_ThrowsInvalidOperationException()
    {
        var parameter = new GenericParameterBuilder("T", ModuleName);

        Should.Throw<InvalidOperationException>(() => GenericImplementationHelper.ImplementType(parameter, new Dictionary<ITypeInfo, ITypeInfo>()));
    }

    /// <summary>
    /// Объявление generic-типа нельзя использовать как тип для реализации.
    /// </summary>
    [Fact]
    public void ImplementType_GenericTypeDefinitionAsParameterType_ThrowsInvalidOperationException()
    {
        var openType = new TypeBuilder("Box", [new GenericParameterBuilder("T", ModuleName)], ModuleName);

        Should.Throw<InvalidOperationException>(() => GenericImplementationHelper.ImplementType(openType, new Dictionary<ITypeInfo, ITypeInfo>()));
    }
}
