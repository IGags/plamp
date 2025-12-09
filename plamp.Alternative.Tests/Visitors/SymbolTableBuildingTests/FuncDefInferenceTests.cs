using System.Linq;
using AutoFixture;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference.Util;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.FuncDefInference;
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
    //2 перегрузки одной функции, всё хорошо
    [InlineData("module test;\n fn x(a: int) {} \n fn x(a: string) {}")]
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
        info.ReturnType.ShouldBe(RuntimeSymbols.SymbolTable.MakeInt());
        var argument = info.ArgumentList.ShouldHaveSingleItem();
        argument.ShouldBe(RuntimeSymbols.SymbolTable.MakeInt());
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
        funcs.Select(x => x.ArgumentTypes[0]).ShouldContain(RuntimeSymbols.SymbolTable.MakeInt());
        funcs.Select(x => x.ArgumentTypes[0]).ShouldContain(RuntimeSymbols.SymbolTable.MakeString());
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

    private SymbolTableBuildingContext SetupAndAct(string code)
    {
        var fixture = new Fixture() { Customizations = { new SymbolTableBuildingContextCustomization([]), new ParserContextCustomization(code) } };
        var parserContext = fixture.Create<ParsingContext>();
        var ast = Parser.ParseFile(parserContext);
        var context = fixture.Create<SymbolTableBuildingContext>();
        var visitor = new FuncDefInferenceWeaver();
        var res = visitor.WeaveDiffs(ast, context);
        return res;
    }
}