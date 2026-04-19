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
        res.Exceptions.ShouldBeEmpty();
        res.SymTableBuilder.ListTypes().ShouldBeEmpty();
    }

    [Fact]
    //У типа есть 1 дженерик, имя которого не совпадает с именем типа
    public void TypeHasSingleGeneric_Correct()
    {
        var code = """
                   module test;
                   type A[T] {}
                   """;
        
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        
        type.IsGenericTypeDefinition.ShouldBeTrue();
        type.IsGenericType.ShouldBeFalse();
        type.IsGenericTypeParameter.ShouldBeFalse();

        var param = type.GetGenericParameters().ShouldHaveSingleItem();
        param.Name.ShouldBe("T");

        param.IsGenericType.ShouldBeFalse();
        param.IsGenericTypeDefinition.ShouldBeFalse();
        param.IsGenericTypeParameter.ShouldBeTrue();
        
        param.IsArrayType.ShouldBeFalse();
        
        param.Fields.ShouldBeEmpty();
    }

    [Fact]
    //У типа есть дженерик имя которого совпадает с именем типа.
    public void GenericParameterHasSameNameAsDefiningType_ReturnsException()
    {
        var code = """
                   module test;
                   type A[A] {}
                   """;
        var res = SetupAndAct(code);
        
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        type.GetGenericParameters().ShouldBeEmpty();
        
        var exception = res.Exceptions.ShouldHaveSingleItem();
        exception.Code.ShouldBe(PlampExceptionInfo.GenericParameterNameSameAsDefiningType().Code);
    }

    [Fact]
    //Дженерик параметр имеет имя совпадающее с именем встроенного типа
    public void GenericParameterHasSameNameAsBuiltinType_ReturnsException()
    {
        var code = """
                   module test;
                   type A[char] {}
                   """;
        
        var res = SetupAndAct(code);
        
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        type.GetGenericParameters().ShouldBeEmpty();
        
        var exception = res.Exceptions.ShouldHaveSingleItem();
        exception.Code.ShouldBe(PlampExceptionInfo.GenericParameterHasSameNameAsBuiltinType().Code);
    }

    [Fact]
    //Два дублирующихся по имени дженерик параметра - ошибка
    public void TwoDuplicateGenericParameters_ReturnsException()
    {
        var code = """
                   module test;
                   type A[B, B] {}
                   """;

        var res = SetupAndAct(code);
        
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        type.GetGenericParameters().ShouldBeEmpty();
        
        res.Exceptions.Count.ShouldBe(2);
        
        res.Exceptions.Select(x => x.Code).ShouldAllBe(x => x == PlampExceptionInfo.DuplicateGenericParameterName().Code);
    }

    [Fact]
    //Два корректных дженерик параметра в типе
    public void TwoGenericParameters_Correct()
    {
        var code = """
                   module test;
                   type A[B, C] {}
                   """;
        
        var res = SetupAndAct(code);
        
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        var generics = type.GetGenericParameters();
        generics.Count.ShouldBe(2);

        foreach (var generic in generics)
        {
            generic.IsGenericTypeParameter.ShouldBeTrue();
            generic.IsGenericTypeDefinition.ShouldBeFalse();
            generic.IsGenericType.ShouldBeFalse();
            
            generic.Fields.ShouldBeEmpty();
        }
        
        var names = new[] {"B", "C"};
        var genericNames = generics.Select(x => x.Name).ToArray();
        names.ShouldAllBe(x => genericNames.Contains(x));
        
        res.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    //Два параметра имеют одинаковое имя с объявляющим типом
    public void TwoGenericParamsHasSameNameAsDefiningType_ReturnsException()
    {
        var code = """
                   module test;
                   type A[A, A] {}
                   """;
        
        var res = SetupAndAct(code);
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        type.GetGenericParameters().ShouldBeEmpty();

        res.Exceptions.ShouldAllBe(x => x.Code == PlampExceptionInfo.GenericParameterNameSameAsDefiningType().Code);
    }

    [Fact]
    public void TwoGenericParametersHasSameNameAsBuiltinType_ReturnsException()
    {
        var code = """
                   module test;
                   type A[byte, byte] {}
                   """;
        
        var res = SetupAndAct(code);
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        type.GetGenericParameters().ShouldBeEmpty();
        
        res.Exceptions.ShouldAllBe(x => x.Code == PlampExceptionInfo.GenericParameterHasSameNameAsBuiltinType().Code);
    }

    [Fact]
    public void TwoTypesWithSameNameAndDifferentGenericCount_ReturnsException()
    {
        var code = """
                   module test;
                   type A;
                   type A[B];
                   """;
        
        var res = SetupAndAct(code);
        res.SymTableBuilder.ListTypes().ShouldBeEmpty();
        res.Exceptions.ShouldBeEmpty();
    }
    
    private SymbolTableBuildingContext SetupAndAct(string code)
    {
        var weaver = new TypedefInferenceWeaver();
        var (ctx, _) = CompilationPipelineBuilder.RunSymTableVisitors(
            code,
            [(ast, ctx) => weaver.WeaveDiffs(ast, ctx)]
        );
        return ctx;
    }
}