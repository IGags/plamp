using System;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Alternative.SymbolsBuildingImpl;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.SymbolsBuildingImpl;

public class BlankFuncInfoTests
{
    /// <summary>
    /// Проверка, что имя обычной функции форматируется определённым образом
    /// </summary>
    [Fact]
    public void SimpleFuncName_ReturnsSameNameAsDefined()
    {
        var info = new BlankFuncInfo("fff", [], Builtins.Void, "test");
        info.Name.ShouldBe("fff()");
        info.DefinitionName.ShouldBe("fff");
        info.IsGenericFunc.ShouldBe(false);
        info.IsGenericFuncDefinition.ShouldBe(false);
        info.GetGenericArguments().ShouldBeEmpty();
        info.GetGenericParameters().ShouldBeEmpty();
        info.GetGenericFuncDefinition().ShouldBeNull();
        info.Arguments.ShouldBeEmpty();
        info.ModuleName.ShouldBe("test");
    }

    /// <summary>
    /// Возвращаемый тип не должен отражаться при выводе имени функции
    /// </summary>
    [Fact]
    public void FuncNonNullReturnType_TypeDoesNotAppear()
    {
        var info = new BlankFuncInfo("ff", [], Builtins.Int, "test");
        info.Name.ShouldBe("ff()");
        info.DefinitionName.ShouldBe("ff");
        info.IsGenericFunc.ShouldBe(false);
        info.IsGenericFuncDefinition.ShouldBe(false);
        info.GetGenericArguments().ShouldBeEmpty();
        info.GetGenericParameters().ShouldBeEmpty();
        info.GetGenericFuncDefinition().ShouldBeNull();
        info.Arguments.ShouldBeEmpty();
        info.ModuleName.ShouldBe("test");
    }

    /// <summary>
    /// Аргументы при выводе имени функции должны быть форматированы определённым образом
    /// </summary>
    [Fact]
    public void FuncWithArgs_ReturnsInParensCommaSeparated()
    {
        var info = new BlankFuncInfo("ff", 
            [new BlankArgInfo("f", Builtins.Int), new BlankArgInfo("s", Builtins.String)], 
            Builtins.Void, "test");
        info.Name.ShouldBe($"ff({Builtins.Int.Name}, {Builtins.String.Name})");
        info.DefinitionName.ShouldBe("ff");
        info.IsGenericFunc.ShouldBe(false);
        info.IsGenericFuncDefinition.ShouldBe(false);
        info.GetGenericArguments().ShouldBeEmpty();
        info.GetGenericParameters().ShouldBeEmpty();
        info.GetGenericFuncDefinition().ShouldBeNull();
        info.Arguments.Count.ShouldBe(2);
        info.ModuleName.ShouldBe("test");
    }

    /// <summary>
    /// Имя дженерик функции должно быть форматировано определённым образом
    /// </summary>
    [Fact]
    public void FuncWithGenericDefs_ReturnsParamNamesInSquareBraces()
    {
        var info = new BlankFuncInfo("ff", [], Builtins.Void,
            [new GenericParameterBuilder("T", "test"), new GenericParameterBuilder("T2", "test")], "test");
        info.Name.ShouldBe("ff[T, T2]()");
        info.DefinitionName.ShouldBe("ff");
        info.IsGenericFunc.ShouldBe(false);
        info.IsGenericFuncDefinition.ShouldBe(true);
        info.GetGenericArguments().ShouldBeEmpty();
        info.GetGenericParameters().ShouldNotBeEmpty();
        info.GetGenericFuncDefinition().ShouldBeNull();
        info.Arguments.ShouldBeEmpty();
        info.ModuleName.ShouldBe("test");
    }

    /// <summary>
    /// Дженерик функция не может иметь 2 одноимённых дженерик параметра
    /// </summary>
    [Fact]
    public void CreateInfoWithDuplicateGenericParams_Throws()
    {
        Should.Throw<InvalidOperationException>(() => new BlankFuncInfo("ff", [], Builtins.Void,
            [new GenericParameterBuilder("T", "test"), new GenericParameterBuilder("T", "test")], "test"));
    }

    /// <summary>
    /// Объявление дженерик функции не может иметь параметр, который объявлен в другом модуле
    /// </summary>
    [Fact]
    public void CreateWithGenericParamFromOtherModule_Throws()
    {
        Should.Throw<InvalidOperationException>(() => new BlankFuncInfo("ff", [], Builtins.Void,
            [new GenericParameterBuilder("T", "test2")], "test"));
    }

    /// <summary>
    /// Не устанавливать .net представление в функцию и попробовать его получить
    /// </summary>
    [Fact]
    public void AsFuncEmpty_Throws()
    {
        var info = new BlankFuncInfo("fff", [], Builtins.Void, "test");
        Should.Throw<NullReferenceException>(info.AsFunc);
    }

    /// <summary>
    /// Установить динамический билдер и попробовать выразить такую функцию в .net представление
    /// </summary>
    [Fact]
    public void AsFuncWithBuilder_Correct()
    {
        var info = new BlankFuncInfo("fff", [], Builtins.Void, "test");
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("123"), AssemblyBuilderAccess.RunAndCollect);
        var mod = asm.DefineDynamicModule("421");
        var method = mod.DefineGlobalMethod("214", MethodAttributes.Static, CallingConventions.Standard, null, []);
        info.MethodBuilder = method;
        var res = info.AsFunc();
        res.ShouldBe(method);
    }

    /// <summary>
    /// Функция котороя не дженерик объявление должна возвращать null на имплементацию дженерика
    /// </summary>
    [Fact]
    public void MakeGenericFuncFromNonGeneric_ReturnsNull()
    {
        var info = new BlankFuncInfo("fff", [], Builtins.Void, "test");
        var res = info.MakeGenericFunc([Builtins.Int]);
        res.ShouldBeNull();
    }

    /// <summary>
    /// Корректный сценарий создания дженерик функции из дженерик объявления
    /// </summary>
    [Fact]
    public void MakeGenericFuncFromGenericDef_Correct()
    {
        var info = new BlankFuncInfo("ff", [], Builtins.Void, [new GenericParameterBuilder("T", "test")], "test");
        var genInfo = info.MakeGenericFunc([Builtins.Int]);
        genInfo.ShouldNotBeNull();
        genInfo.ShouldNotBe(info);
    }

    /// <summary>
    /// Эквивалентность функций определяется по имени/модулю, а не по внутренним билдерам и тд и тп
    /// </summary>
    [Fact]
    public void EqualityDoesNotDependOnBuilder_Correct()
    {
        var info = new BlankFuncInfo("fff", [], Builtins.Void, "test");
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("123"), AssemblyBuilderAccess.RunAndCollect);
        var mod = asm.DefineDynamicModule("421");
        var method = mod.DefineGlobalMethod("214", MethodAttributes.Static, CallingConventions.Standard, null, []);
        info.MethodBuilder = method;
        
        var info2 = new BlankFuncInfo("fff", [], Builtins.Void, "test");
        info.ShouldBe(info2);
    }

    /// <summary>
    /// Функция с пустым именем не может существовать
    /// </summary>
    [Fact]
    public void MakeFuncWithEmptyName_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() => new BlankFuncInfo("", [], Builtins.Void, "test"));
    }
}