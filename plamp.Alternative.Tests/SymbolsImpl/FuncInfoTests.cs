using System;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsImpl;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.SymbolsImpl;

public class FuncInfoTests
{
    private const string ModuleName = "test";

    private class FuncHost
    {
        public static int Sum(int left, int right) => left + right;

        public static string Join(string[] items) => string.Join("", items);

        public static T Echo<T>(T value) => value;

        public static (TKey, TValue) Pair<TKey, TValue>(TKey key, TValue value) => (key, value);
    }

    /// <summary>
    /// Нельзя создать функцию без имени модуля
    /// </summary>
    [Fact]
    public void CreateWithEmptyModuleName_ThrowsInvalidOperationException()
    {
        var method = typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(string)])!;

        Should.Throw<InvalidOperationException>(() => new FuncInfo(method, ""));
    }

    /// <summary>
    /// Имя модуля не может состоять из пробелов
    /// </summary>
    [Fact]
    public void CreateWithWhitespaceModuleName_ThrowsInvalidOperationException()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Sum))!;

        Should.Throw<InvalidOperationException>(() => new FuncInfo(method, "   "));
    }

    /// <summary>
    /// Закрытый generic-метод не может быть функцией в таблице символов
    /// </summary>
    [Fact]
    public void CreateWithGenericImplementation_ThrowsInvalidOperationException()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Echo))!.MakeGenericMethod(typeof(int));

        Should.Throw<InvalidOperationException>(() => new FuncInfo(method, ModuleName));
    }

    /// <summary>
    /// Обычная функция возвращает имя с типами аргументов
    /// </summary>
    [Fact]
    public void SimpleFuncOverall_Correct()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Sum))!;
        var info = new FuncInfo(method, ModuleName);

        info.Name.ShouldBe("Sum(Int32, Int32)");
        info.DefinitionName.ShouldBe(nameof(FuncHost.Sum));
        info.ModuleName.ShouldBe(ModuleName);
        info.IsGenericFunc.ShouldBeFalse();
        info.IsGenericFuncDefinition.ShouldBeFalse();
    }

    /// <summary>
    /// Возвращает исходный runtime-метод
    /// </summary>
    [Fact]
    public void AsFunc_ReturnsRuntimeMethod()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Sum))!;
        var info = new FuncInfo(method, ModuleName);

        info.AsFunc().ShouldBe(method);
    }

    /// <summary>
    /// Возвращаемый тип создаётся с тем же модулем
    /// </summary>
    [Fact]
    public void ReturnType_ReturnsTypeInfoWithSameModule()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Sum))!;
        var info = new FuncInfo(method, ModuleName);

        info.ReturnType.ModuleName.ShouldBe(ModuleName);
        info.ReturnType.AsType().ShouldBe(typeof(int));
    }

    /// <summary>
    /// Аргументы создаются с именами и типами runtime-параметров
    /// </summary>
    [Fact]
    public void Arguments_ReturnRuntimeParameters()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Sum))!;
        var info = new FuncInfo(method, ModuleName);

        var args = info.Arguments;

        args.Count.ShouldBe(2);
        args[0].Name.ShouldBe("left");
        args[0].Type.ModuleName.ShouldBe(ModuleName);
        args[0].Type.AsType().ShouldBe(typeof(int));
        args[1].Name.ShouldBe("right");
        args[1].Type.AsType().ShouldBe(typeof(int));
    }

    /// <summary>
    /// Массив в аргументах корректно сохраняется
    /// </summary>
    [Fact]
    public void ArgumentsWithArrayType_ReturnArrayTypeInfo()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Join))!;
        var info = new FuncInfo(method, ModuleName);

        var argType = info.Arguments.ShouldHaveSingleItem().Type;

        argType.ModuleName.ShouldBe(ModuleName);
        argType.IsArrayType.ShouldBeTrue();
        argType.ElementType().ShouldNotBeNull().AsType().ShouldBe(typeof(string));
        argType.AsType().ShouldBe(typeof(string[]));
    }

    /// <summary>
    /// Generic-объявление возвращает имена параметров в имени функции
    /// </summary>
    [Fact]
    public void GenericFuncOverall_Correct()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Pair))!;
        var info = new FuncInfo(method, ModuleName);

        info.Name.ShouldBe("Pair[TKey, TValue](TKey, TValue)");
        info.DefinitionName.ShouldBe(nameof(FuncHost.Pair));
        info.IsGenericFunc.ShouldBeFalse();
        info.IsGenericFuncDefinition.ShouldBeTrue();
    }

    /// <summary>
    /// Generic-параметры возвращаются для generic-объявления
    /// </summary>
    [Fact]
    public void GetGenericParametersForGenericDefinition_ReturnsParameters()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Pair))!;
        var info = new FuncInfo(method, ModuleName);

        var parameters = info.GetGenericParameters();

        parameters.Count.ShouldBe(2);
        parameters[0].Name.ShouldBe("TKey");
        parameters[0].ModuleName.ShouldBe(ModuleName);
        parameters[0].IsGenericTypeParameter.ShouldBeTrue();
        parameters[1].Name.ShouldBe("TValue");
    }

    /// <summary>
    /// У runtime-функции нет generic-аргументов и объявления-родителя
    /// </summary>
    [Fact]
    public void GenericImplementationInfoCollections_AreEmpty()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Pair))!;
        var info = new FuncInfo(method, ModuleName);

        info.GetGenericArguments().ShouldBeEmpty();
        info.GetGenericFuncDefinition().ShouldBeNull();
    }

    /// <summary>
    /// Обычная функция не создаёт generic-реализацию
    /// </summary>
    [Fact]
    public void MakeGenericFuncFromNonGeneric_ReturnsNull()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Sum))!;
        var info = new FuncInfo(method, ModuleName);

        info.MakeGenericFunc([Builtins.Int]).ShouldBeNull();
    }

    /// <summary>
    /// Generic-объявление создаёт закрытую функцию
    /// </summary>
    [Fact]
    public void MakeGenericFuncFromGenericDefinition_ReturnsImplementation()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Echo))!;
        var info = new FuncInfo(method, ModuleName);

        var impl = info.MakeGenericFunc([Builtins.Int]).ShouldNotBeNull();

        impl.IsGenericFunc.ShouldBeTrue();
        impl.IsGenericFuncDefinition.ShouldBeFalse();
        impl.GetGenericFuncDefinition().ShouldBe(info);
        impl.GetGenericArguments().ShouldHaveSingleItem().ShouldBe(Builtins.Int);
        impl.ReturnType.ShouldBe(Builtins.Int);
        impl.Arguments.ShouldHaveSingleItem().Type.ShouldBe(Builtins.Int);
        impl.AsFunc().ShouldBe(method.MakeGenericMethod(typeof(int)));
    }

    /// <summary>
    /// Функции с одинаковым runtime-методом равны
    /// </summary>
    [Fact]
    public void EqualsWithSameRuntimeMethod_ReturnsTrue()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Sum))!;
        IFnInfo info = new FuncInfo(method, ModuleName);
        IFnInfo sameInfo = new FuncInfo(method, "otherModule");

        info.Equals(sameInfo).ShouldBeTrue();
    }

    /// <summary>
    /// Разные runtime-методы не равны
    /// </summary>
    [Fact]
    public void EqualsWithDifferentRuntimeMethod_ReturnsFalse()
    {
        IFnInfo info = new FuncInfo(typeof(FuncHost).GetMethod(nameof(FuncHost.Sum))!, ModuleName);
        IFnInfo otherInfo = new FuncInfo(typeof(FuncHost).GetMethod(nameof(FuncHost.Join))!, ModuleName);

        info.Equals(otherInfo).ShouldBeFalse();
    }

    /// <summary>
    /// Сравнение с null возвращает false
    /// </summary>
    [Fact]
    public void EqualsWithNull_ReturnsFalse()
    {
        IFnInfo info = new FuncInfo(typeof(FuncHost).GetMethod(nameof(FuncHost.Sum))!, ModuleName);

        info.Equals(null).ShouldBeFalse();
    }

    /// <summary>
    /// Hash code основан на runtime-методе
    /// </summary>
    [Fact]
    public void HashCodeWithSameRuntimeMethod_Correct()
    {
        var method = typeof(FuncHost).GetMethod(nameof(FuncHost.Sum))!;
        var info = new FuncInfo(method, ModuleName);
        var sameInfo = new FuncInfo(method, "otherModule");

        info.GetHashCode().ShouldBe(sameInfo.GetHashCode());
    }
}
