using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsImpl;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.SymbolsImpl;

public class FldInfoTests
{
    private const string ModuleName = "test";

    private class FieldHost
    {
        public int Number = 1;
        public string Text = "";
        public int[] Numbers = [];
        public KeyValuePair<string, int> Pair = new("", 0);
    }

    /// <summary>
    /// Нельзя создать поле без имени модуля
    /// </summary>
    [Fact]
    public void CreateWithEmptyModuleName_ThrowsInvalidOperationException()
    {
        var field = typeof(DateTime).GetFields().First();

        Should.Throw<InvalidOperationException>(() => new FldInfo(field, ""));
    }

    /// <summary>
    /// Имя модуля не может состоять из пробелов
    /// </summary>
    [Fact]
    public void CreateWithWhitespaceModuleName_ThrowsInvalidOperationException()
    {
        var field = typeof(FieldHost).GetField(nameof(FieldHost.Number))!;

        Should.Throw<InvalidOperationException>(() => new FldInfo(field, "   "));
    }

    /// <summary>
    /// Имя поля берётся из runtime-поля
    /// </summary>
    [Fact]
    public void Name_ReturnsRuntimeFieldName()
    {
        var field = typeof(FieldHost).GetField(nameof(FieldHost.Number))!;
        var info = new FldInfo(field, ModuleName);

        info.Name.ShouldBe(nameof(FieldHost.Number));
    }

    /// <summary>
    /// Возвращает исходное runtime-поле
    /// </summary>
    [Fact]
    public void AsField_ReturnsRuntimeField()
    {
        var field = typeof(FieldHost).GetField(nameof(FieldHost.Text))!;
        var info = new FldInfo(field, ModuleName);

        info.AsField().ShouldBe(field);
    }

    /// <summary>
    /// Тип поля создаётся с тем же модулем
    /// </summary>
    [Fact]
    public void FieldType_ReturnsTypeInfoWithSameModule()
    {
        var field = typeof(FieldHost).GetField(nameof(FieldHost.Number))!;
        var info = new FldInfo(field, ModuleName);

        var fieldType = info.FieldType;

        fieldType.ModuleName.ShouldBe(ModuleName);
        fieldType.Name.ShouldBe(nameof(Int32));
        fieldType.AsType().ShouldBe(typeof(int));
    }

    /// <summary>
    /// Тип массива корректно сохраняется
    /// </summary>
    [Fact]
    public void FieldTypeForArray_ReturnsArrayTypeInfo()
    {
        var field = typeof(FieldHost).GetField(nameof(FieldHost.Numbers))!;
        var info = new FldInfo(field, ModuleName);

        var fieldType = info.FieldType;

        fieldType.ModuleName.ShouldBe(ModuleName);
        fieldType.IsArrayType.ShouldBeTrue();
        fieldType.ElementType().ShouldNotBeNull().AsType().ShouldBe(typeof(int));
        fieldType.AsType().ShouldBe(typeof(int[]));
    }

    /// <summary>
    /// Закрытый generic-тип поля корректно сохраняется
    /// </summary>
    [Fact]
    public void FieldTypeForGenericImplementation_ReturnsGenericTypeInfo()
    {
        var field = typeof(FieldHost).GetField(nameof(FieldHost.Pair))!;
        var info = new FldInfo(field, ModuleName);

        var fieldType = info.FieldType;

        fieldType.ModuleName.ShouldBe(ModuleName);
        fieldType.IsGenericType.ShouldBeTrue();
        fieldType.GetGenericArguments().Select(x => x.AsType()).ShouldBe([typeof(string), typeof(int)]);
        fieldType.AsType().ShouldBe(typeof(KeyValuePair<string, int>));
    }

    /// <summary>
    /// Поля с одинаковым runtime-представлением равны
    /// </summary>
    [Fact]
    public void EqualsWithSameRuntimeField_ReturnsTrue()
    {
        var field = typeof(FieldHost).GetField(nameof(FieldHost.Number))!;
        IFieldInfo info = new FldInfo(field, ModuleName);
        IFieldInfo sameInfo = new FldInfo(field, "otherModule");

        info.Equals(sameInfo).ShouldBeTrue();
    }

    /// <summary>
    /// Разные runtime-поля не равны
    /// </summary>
    [Fact]
    public void EqualsWithDifferentRuntimeField_ReturnsFalse()
    {
        IFieldInfo info = new FldInfo(typeof(FieldHost).GetField(nameof(FieldHost.Number))!, ModuleName);
        IFieldInfo otherInfo = new FldInfo(typeof(FieldHost).GetField(nameof(FieldHost.Text))!, ModuleName);

        info.Equals(otherInfo).ShouldBeFalse();
    }

    /// <summary>
    /// Сравнение с null возвращает false
    /// </summary>
    [Fact]
    public void EqualsWithNull_ReturnsFalse()
    {
        IFieldInfo info = new FldInfo(typeof(FieldHost).GetField(nameof(FieldHost.Number))!, ModuleName);

        info.Equals(null).ShouldBeFalse();
    }
}
