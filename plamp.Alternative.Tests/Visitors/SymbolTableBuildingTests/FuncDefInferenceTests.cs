using System.Linq;
using plamp.Abstractions.Symbols.SymTableBuilding;
using plamp.Alternative.Visitors.SymbolTableBuilding;
using plamp.Alternative.Visitors.SymbolTableBuilding.FuncDefInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.SymbolTableBuildingTests;

/// <summary>
/// Проверяет вывод сигнатур функций в таблицу символов.
/// </summary>
public class FuncDefInferenceTests
{
    /// <summary>
    /// Функций нет в модуле - корректно.
    /// </summary>
    [Fact]
    public void InferenceEmptyDef_Correct()
    {
        var code = "module test;";
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        res.SymTableBuilder.ListFuncs().ShouldBeEmpty();
    }

    /// <summary>
    /// В модуле одна корректная функция.
    /// </summary>
    [Fact]
    public void InferenceSingleFunction_Correct()
    {
        var code = """
                   module test;
                   fn a(x: int) int {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        var item = res.SymTableBuilder.ListFuncs().ShouldHaveSingleItem();
        item.Name.ShouldBe("a(int)");
        var argument = item.Arguments.ShouldHaveSingleItem();
        argument.Name.ShouldBe("x");
        argument.Type.ShouldBe(Builtins.Int);
    }

    /// <summary>
    /// Две функции с одинаковой сигнатурой не добавляются в модуль без ошибки.
    /// </summary>
    [Fact]
    public void InferenceTwoIdenticalSignatures_ReturnsErrors()
    {
        var code = """
                   module test;
                   fn a(x: int) int {}
                   fn a(x: int) string {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(0);
        res.SymTableBuilder.ListFuncs().ShouldBeEmpty();
    }
    
    /// <summary>
    /// Две функции с одинаковым именем не добавляются в модуль без ошибки.
    /// </summary>
    [Fact]
    public void InferenceTwoIdenticalNames_ReturnsErrors()
    {
        var code = """
                   module test;
                   fn a(x: int) int {}
                   fn a() string {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(0);
        res.SymTableBuilder.ListFuncs().ShouldBeEmpty();
    }

    /// <summary>
    /// Функции без возвращаемого значения проставляется void.
    /// </summary>
    [Fact]
    public void InferenceVoidFunction_Correct()
    {
        var code = """
                   module test;
                   fn a() {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(0);
        var fn = res.SymTableBuilder.ListFuncs().ShouldHaveSingleItem();
        fn.ReturnType.ShouldBe(Builtins.Void);
    }

    /// <summary>
    /// Функция с неизвестным типом результата не добавляется в таблицу символов.
    /// </summary>
    [Fact]
    public void InferenceUnknownReturnTypeFunction_DoesNotAddToSymbols()
    {
        var code = """
                   module test;
                   fn a() MadeUpType {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(1);
        res.SymTableBuilder.ListFuncs().ShouldBeEmpty();
    }

    /// <summary>
    /// Функция с неизвестным типом аргумента не добавляется в таблицу символов.
    /// </summary>
    [Fact]
    public void InferenceUnknownArgTypeFunction_DoesNotAddToSymbols()
    {
        var code = """
                   module test;
                   fn a(x: MadeUpType) {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(1);
        res.SymTableBuilder.ListFuncs().ShouldBeEmpty();
    }

    /// <summary>
    /// Функция с повторяющимся именем аргумента не добавляется в таблицу символов.
    /// </summary>
    [Fact]
    public void InferenceDuplicateArgNameFunction_SkipAdding()
    {
        var code = """
                   module test;
                   fn a(x, x: int) {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(2);
        res.Exceptions.All(x => x.Code == PlampExceptionInfo.DuplicateParameterName().Code).ShouldBe(true);
        res.SymTableBuilder.ListFuncs().ShouldBeEmpty();
    }

    /// <summary>
    /// Функция с generic-параметрами корректно добавляется в таблицу символов.
    /// </summary>
    [Fact]
    public void InferenceFuncWithGenericParams_Correct()
    {
        var code = """
                   module test;
                   fn a[T, V](f: T) V {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        var fn = res.SymTableBuilder.ListFuncs().ShouldHaveSingleItem();
        
        fn.ReturnType.ShouldBeAssignableTo<IGenericParameterBuilder>().Name.ShouldBe("V");
        fn.Arguments.ShouldHaveSingleItem().Type.ShouldBeAssignableTo<IGenericParameterBuilder>().Name.ShouldBe("T");
        fn.GetGenericParameters().Count.ShouldBe(2);
    }

    /// <summary>
    /// Функция с повторяющимся именем generic-параметра возвращает ошибки и не добавляется.
    /// </summary>
    [Fact]
    public void InferenceFuncWithDupGenericArgName_ReturnsExceptionSkipAdding()
    {
        var code = """
                   module test;
                   fn a[T, T]() {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(2);
        res.Exceptions.All(x => x.Code == PlampExceptionInfo.DuplicateGenericParameterName().Code).ShouldBe(true);
    }

    /// <summary>
    /// Имя generic-параметра функции не должно совпадать с именем встроенного типа.
    /// </summary>
    [Fact]
    public void InferenceFuncWithGenericParamSameNameAsBuiltinType_ReturnsException()
    {
        var code = """
                   module test;
                   fn a[int]() {}
                   """;

        var res = SetupAndAct(code);

        var fn = res.SymTableBuilder.ListFuncs().ShouldHaveSingleItem();
        fn.GetGenericParameters().ShouldBeEmpty();

        var exception = res.Exceptions.ShouldHaveSingleItem();
        exception.Code.ShouldBe(PlampExceptionInfo.GenericParameterHasSameNameAsBuiltinMember().Code);
    }

    /// <summary>
    /// Имя generic-параметра функции не должно совпадать с именем самой функции.
    /// </summary>
    [Fact]
    public void InferenceFuncWithGenericParamSameNameAsFunction_ReturnsException()
    {
        var code = """
                   module test;
                   fn a[a]() {}
                   """;

        var res = SetupAndAct(code);

        var fn = res.SymTableBuilder.ListFuncs().ShouldHaveSingleItem();
        fn.GetGenericParameters().ShouldBeEmpty();

        var exception = res.Exceptions.ShouldHaveSingleItem();
        exception.Code.ShouldBe(PlampExceptionInfo.GenericParamSameNameAsDefiningFunction().Code);
    }

    private SymbolTableBuildingContext SetupAndAct(string code)
    {
        var weaver = new FuncDefInferenceWeaver();
        var (ctx, _) = CompilationPipelineBuilder.RunSymTableVisitors(
                code,
                [weaver.WeaveDiffs]
            );
        return ctx;
    }
}
