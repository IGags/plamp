using System.Linq;
using plamp.Abstractions.Symbols.SymTableBuilding;
using plamp.Alternative.Visitors.SymbolTableBuilding;
using plamp.Alternative.Visitors.SymbolTableBuilding.FuncDefInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.SymbolTableBuildingTests;


//У функции returnType == null - должен проставить void
//Тип возвращаемого значения неизвестен, возврат ошибки
//Тип аргумента неизвестен, возврат ошибки
public class FuncDefInferenceTests
{
    [Fact]
    //Функций нет в модуле - корректно
    public void InferenceEmptyDef_Correct()
    {
        var code = "module test;";
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        res.SymTableBuilder.ListFuncs().ShouldBeEmpty();
    }

    [Fact]
    //В модуле одна корректная функция
    public void InferenceSingleFunction_Correct()
    {
        var code = """
                   module test;
                   fn a(x: int) int {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        var item = res.SymTableBuilder.ListFuncs().ShouldHaveSingleItem();
        item.Name.ShouldBe("a");
        var argument = item.Arguments.ShouldHaveSingleItem();
        argument.Name.ShouldBe("x");
        argument.Type.ShouldBe(Builtins.Int);
    }

    [Fact]
    //Две одинаковых функции ничего не будет добавлено в модуль, ошибка выведена НЕ будет
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
    
    [Fact]
    //Две одинаковых функции по имени ничего не будет добавлено в модуль, ошибка выведена НЕ будет
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

    [Fact]
    //Функция с возвращаемым значением void - визитор должен добавить тип
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

    [Fact]
    //У функции неизвестный тип возвращаемого значения, поэтому мы не добавляем такую функцию
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

    [Fact]
    //У функции неизвестный тип аргумента, такую функцию нельзя добавить в таблицу символов
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

    [Fact]
    //У функции 2 аргумента с одинаковым именем, функции не будет в таблице символов
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

    [Fact]
    //Фнкция с дженериком, корректно
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
        fn.GenericParams.Count.ShouldBe(2);
    }

    [Fact]
    //Функция с дублирующимся именем дженерика, не добавляется в таблицу, возвращает ошибки
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

    private SymbolTableBuildingContext SetupAndAct(string code)
    {
        var weaver = new FuncDefInferenceWeaver();
        var (ctx, _) = CompilationPipelineBuilder.RunSymTableVisitors(
                code,
                [(ast, ctx) => weaver.WeaveDiffs(ast, ctx)]
            );
        return ctx;
    }
}