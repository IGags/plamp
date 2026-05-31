using System;
using System.Collections.Generic;
using plamp.Alternative.SymbolsBuildingImpl;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.SymbolsBuildingImpl;

public class GenericParameterBuilderTests
{
    /// <summary>
    /// Не пустое имя
    /// </summary>
    [Fact]
    public void CreateParameterBuilderEmptyName_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() => new GenericParameterBuilder("", "test"));
    }

    /// <summary>
    /// Не пустое имя модуля
    /// </summary>
    [Fact]
    public void CreateGenericParameterEmptyModule_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() => new GenericParameterBuilder("T", ""));
    }

    /// <summary>
    /// Name == DefName
    /// </summary>
    [Fact]
    public void CreateGenericParameter_NameMustEqualsDefName()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        parameter.Name.ShouldBe("T");
        parameter.DefinitionName.ShouldBe("T");
    }

    /// <summary>
    /// У дженерик параметра нет полей
    /// </summary>
    [Fact]
    public void Fields_ShouldBeEmpty()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        parameter.Fields.ShouldBeEmpty();
    }

    /// <summary>
    /// Дженерик параметр не является массивом
    /// </summary>
    [Fact]
    public void IsArrayType_ShouldBeFalse()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        parameter.IsArrayType.ShouldBeFalse();
    }

    /// <summary>
    /// Дженерик параметр не является объявлением дженерик типа
    /// </summary>
    [Fact]
    public void IsGenericTypeDefinition_ShouldBeFalse()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        parameter.IsGenericTypeDefinition.ShouldBeFalse();
    }

    /// <summary>
    /// Дженерик параметр не является реализацией дженерик типа
    /// </summary>
    [Fact]
    public void IsGenericType_ShouldBeFalse()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        parameter.IsGenericType.ShouldBeFalse();
    }

    /// <summary>
    /// Тип является дженерик параметром
    /// </summary>
    [Fact]
    public void IsGenericTypeParameter_ShouldBeTrue()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        parameter.IsGenericTypeParameter.ShouldBeTrue();
    }

    /// <summary>
    /// Без .net реализации бросает исключение
    /// </summary>
    [Fact]
    public void AsTypeWithoutNetImplementation_Throws()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        Should.Throw<InvalidOperationException>(parameter.AsType);
    }

    /// <summary>
    /// С реализацией возвращает всё корректно
    /// </summary>
    [Fact]
    public void AsTypeWithNetImplementation_ReturnsType()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        var type = typeof(Dictionary<,>).GetGenericArguments()[0];

        parameter.GenericParameterType = type;

        parameter.AsType().ShouldBe(type);
    }

    /// <summary>
    /// При установке type запрещает дальнейшую модификацию
    /// </summary>
    [Fact]
    public void SetGenericParameterType_PreventsFurtherModification()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        parameter.GenericParameterType = typeof(Dictionary<,>).GetGenericArguments()[0];

        Should.Throw<InvalidOperationException>(() => parameter.GenericParameterType = typeof(Dictionary<,>).GetGenericArguments()[1]);
        Should.Throw<InvalidOperationException>(() => parameter.ParameterBuilder = null);
        Should.Throw<InvalidOperationException>(() => _ = parameter.ParameterBuilder);
    }

    /// <summary>
    /// Позволяет построить тип массива
    /// </summary>
    [Fact]
    public void MakeArrayType_ReturnsArrayType()
    {
        var parameter = new GenericParameterBuilder("T", "test");

        var arrayType = parameter.MakeArrayType();

        arrayType.ShouldBeAssignableTo<ArrayTypeBuilder>();
        arrayType.IsArrayType.ShouldBeTrue();
        arrayType.ElementType().ShouldBe(parameter);
    }

    /// <summary>
    /// Не позволяет получить тип элемента
    /// </summary>
    [Fact]
    public void ElementType_ReturnsNull()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        parameter.ElementType().ShouldBeNull();
    }

    /// <summary>
    /// Не позволяет получить дженерик объявление
    /// </summary>
    [Fact]
    public void GetGenericTypeDefinition_ReturnsNull()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        parameter.GetGenericTypeDefinition().ShouldBeNull();
    }

    /// <summary>
    /// Не имеет дженерик аргументов
    /// </summary>
    [Fact]
    public void GetGenericArguments_ShouldBeEmpty()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        parameter.GetGenericArguments().ShouldBeEmpty();
    }

    /// <summary>
    /// Не имеет дженерик параметров
    /// </summary>
    [Fact]
    public void GetGenericParameters_ShouldBeEmpty()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        parameter.GetGenericParameters().ShouldBeEmpty();
    }

    /// <summary>
    /// Нельзя построить дженерик тип
    /// </summary>
    [Fact]
    public void MakeGenericType_ReturnsNull()
    {
        var parameter = new GenericParameterBuilder("T", "test");
        parameter.MakeGenericType([new GenericParameterBuilder("U", "test")]).ShouldBeNull();
    }

    /// <summary>
    /// Сравнение не основывается на наличие представления .net
    /// </summary>
    [Fact]
    public void Equality_DoesNotDependOnNetImplementation()
    {
        var parameterWithoutImplementation = new GenericParameterBuilder("T", "test");
        var parameterWithImplementation = new GenericParameterBuilder("T", "test")
        {
            GenericParameterType = typeof(Dictionary<,>).GetGenericArguments()[0]
        };

        parameterWithoutImplementation.Equals(parameterWithImplementation).ShouldBeTrue();
    }
}
