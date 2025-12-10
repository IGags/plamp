using System.Linq;
using plamp.Alternative.Visitors.SymbolTableBuilding;
using plamp.Alternative.Visitors.SymbolTableBuilding.FuncDefInference;
using plamp.Intrinsics;
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
        res.CurrentModuleTable.ListFunctions().ShouldBeEmpty();
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
        var item = res.CurrentModuleTable.ListFunctions().ShouldHaveSingleItem();
        var info = item.GetDefinitionInfo();
        info.Name.ShouldBe("a");
        var argument = info.ArgumentList.ShouldHaveSingleItem();
        argument.ShouldBe(RuntimeSymbols.SymbolTable.Int);
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
        var funcs = res.CurrentModuleTable.ListFunctions();
        funcs.Count.ShouldBe(2);
        funcs.Select(x => x.Name).ShouldAllBe(x => x == "a");
        funcs.Select(x => x.ArgumentTypes[0]).ShouldContain(RuntimeSymbols.SymbolTable.Int);
        funcs.Select(x => x.ArgumentTypes[0]).ShouldContain(RuntimeSymbols.SymbolTable.String);
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
        var fn = res.CurrentModuleTable.ListFunctions().ShouldHaveSingleItem();
        var info = fn.GetDefinitionInfo();
        info.ReturnType.ShouldBe(RuntimeSymbols.SymbolTable.Void);
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
        res.CurrentModuleTable.ListFunctions().ShouldBeEmpty();
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
        res.CurrentModuleTable.ListFunctions().ShouldBeEmpty();
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
        res.CurrentModuleTable.ListFunctions().ShouldHaveSingleItem();
    }

    private SymbolTableBuildingContext SetupAndAct(string code)
    {
        var weaver = new FuncDefInferenceWeaver();
        return CompilationPipelineBuilder.RunSymbolTableBuildingPipeline(
                code,
                [(ast, ctx) => weaver.WeaveDiffs(ast, ctx)]
            );
    }
}