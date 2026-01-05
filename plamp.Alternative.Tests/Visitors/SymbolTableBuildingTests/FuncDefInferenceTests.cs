using System.Linq;
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
    //В модуле 2 перегрузки одной функции
    public void InferenceFunctionsOverloads_Correct()
    {
        var code = """
                   module test;
                   fn a(x: int) int {}
                   fn a(x: string) int {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        var funcs = res.SymTableBuilder.ListFuncs();
        funcs.Count.ShouldBe(2);
        funcs.Select(x => x.Name).ShouldAllBe(x => x == "a");
        funcs.Select(x => x.Arguments[0].Type).ShouldContain(Builtins.Int);
        funcs.Select(x => x.Arguments[0].Type).ShouldContain(Builtins.String);
    }

    [Fact]
    //Два одинаковых объявления, возвращает ошибку
    public void InferenceTwoIdenticalSignatures_ReturnsErrors()
    {
        var code = """
                   module test;
                   fn a(x: int) int {}
                   fn a(x: int) string {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(2);
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
    //У функции 2 аргумента с одинаковым именем, однако функция будет добавлена в таблицу символов
    public void InferenceDuplicateArgNameFunction_AddsToSymbols()
    {
        var code = """
                   module test;
                   fn a(x, x: int) {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(2);
        res.Exceptions.All(x => x.Code == PlampExceptionInfo.DuplicateParameterName().Code).ShouldBe(true);
        res.SymTableBuilder.ListFuncs().ShouldHaveSingleItem();
    }

    private SymbolTableBuildingContext SetupAndAct(string code)
    {
        var weaver = new FuncDefInferenceWeaver();
        return CompilationPipelineBuilder.RunSymTableVisitors(
                code,
                [(ast, ctx) => weaver.WeaveDiffs(ast, ctx)]
            );
    }
}