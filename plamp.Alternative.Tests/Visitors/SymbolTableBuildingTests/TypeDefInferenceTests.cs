using System.Linq;
using plamp.Alternative.Visitors.SymbolTableBuilding;
using plamp.Alternative.Visitors.SymbolTableBuilding.TypedefInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.SymbolTableBuildingTests;

public class TypeDefInferenceTests
{
    [Fact]
    //Пустой модуль - нет ошибок
    public void EmptyModule_Correct()
    {
        var code = """
                   module test;
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    //Модуль с 1 типом - нет ошибок
    public void SingleTypeInModule_Correct()
    {
        var code = """
                   module test;
                   type A {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        var types = res.SymTableBuilder.ListTypes();
        var typ = types.ShouldHaveSingleItem();
        typ.Name.ShouldBe("A");
        typ.Fields.ShouldBeEmpty();
    }

    [Fact]
    //Модуль с типом, имя которого совпадает с именем типа рантайма - ошибка, объявление не будет добавлено
    public void SingeTypeMatchWithRuntimeType_ReturnException()
    {
        var code = """
                   module test;
                   type int {}
                   """;
        var res = SetupAndAct(code);
        var ex = res.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.CannotDefineCoreType().Code);
    }

    [Fact]
    //Модуль с 2мя типами - нет ошибок
    public void TwoTypesDifferentName_Correct()
    {
        var code = """
                   module test;
                   type A {}
                   type B {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        var types = res.SymTableBuilder.ListTypes();
        types.Count.ShouldBe(2);
        var names = new[] { "A", "B" };
        types.Select(x => x.Name).All(names.Contains).ShouldBeTrue();
    }

    [Fact]
    //Модуль с 2мя типами имеющими одинаковые имена - 2 ошибки, типы не будут добавлены
    public void TwoTypesSameName_ReturnsException()
    {
        var code = """
                   module test;
                   type A {}
                   type A {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(2);
        res.Exceptions.Select(x => x.Code).ShouldAllBe(x => x == PlampExceptionInfo.DuplicateTypeDefinition("").Code);
        res.SymTableBuilder.ListTypes().ShouldBeEmpty();
    }
    
    private SymbolTableBuildingContext SetupAndAct(string code)
    {
        var weaver = new TypedefInferenceWeaver();
        return CompilationPipelineBuilder.RunSymbolTableBuildingPipeline(
            code,
            [(ast, ctx) => weaver.WeaveDiffs(ast, ctx)]
        );
    }
}