using System.Linq;
using plamp.Alternative.Visitors.SymbolTableBuilding;
using plamp.Alternative.Visitors.SymbolTableBuilding.TypedefInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.SymbolTableBuildingTests;

/// <summary>
/// Проверяет вывод объявлений типов в таблицу символов.
/// </summary>
public class TypeDefInferenceTests
{
    /// <summary>
    /// Пустой модуль не даёт ошибок.
    /// </summary>
    [Fact]
    public void EmptyModule_Correct()
    {
        var code = """
                   module test;
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Модуль с одним типом корректно добавляет тип в таблицу символов.
    /// </summary>
    [Fact]
    public void SingleTypeInModule_Correct()
    {
        var code = """
                   module test;
                   data A {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        var types = res.SymTableBuilder.ListTypes();
        var typ = types.ShouldHaveSingleItem();
        typ.Name.ShouldBe("A");
        typ.Fields.ShouldBeEmpty();
    }

    /// <summary>
    /// Тип с именем встроенного типа возвращает ошибку.
    /// </summary>
    [Fact]
    public void SingeTypeMatchWithRuntimeType_ReturnException()
    {
        var code = """
                   module test;
                   data int {}
                   """;
        var res = SetupAndAct(code);
        var ex = res.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.CannotDefineCoreType().Code);
    }

    /// <summary>
    /// Два типа с разными именами корректно добавляются в таблицу символов.
    /// </summary>
    [Fact]
    public void TwoTypesDifferentName_Correct()
    {
        var code = """
                   module test;
                   data A {}
                   data B {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        var types = res.SymTableBuilder.ListTypes();
        types.Count.ShouldBe(2);
        var names = new[] { "A", "B" };
        types.Select(x => x.Name).All(names.Contains).ShouldBeTrue();
    }

    /// <summary>
    /// Два типа с одинаковым именем не добавляются в таблицу символов.
    /// </summary>
    [Fact]
    public void TwoTypesSameName_ReturnsException()
    {
        var code = """
                   module test;
                   data A {}
                   data A {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        res.SymTableBuilder.ListTypes().ShouldBeEmpty();
    }

    /// <summary>
    /// Тип с одним корректным generic-параметром добавляется в таблицу символов.
    /// </summary>
    [Fact]
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

    /// <summary>
    /// Имя generic-параметра типа не должно совпадать с именем самого типа.
    /// </summary>
    [Fact]
    public void GenericParameterHasSameNameAsDefiningType_ReturnsException()
    {
        var code = """
                   module test;
                   type A[A] {}
                   """;
        var res = SetupAndAct(code);
        
        res.SymTableBuilder.ListTypes().ShouldBeEmpty();
        
        var exception = res.Exceptions.ShouldHaveSingleItem();
        exception.Code.ShouldBe(PlampExceptionInfo.GenericParameterNameSameAsDefiningType().Code);
    }

    /// <summary>
    /// Имя generic-параметра типа не должно совпадать с именем встроенного типа.
    /// </summary>
    [Fact]
    public void GenericParameterHasSameNameAsBuiltinType_ReturnsException()
    {
        var code = """
                   module test;
                   type A[char] {}
                   """;
        
        var res = SetupAndAct(code);
        
        res.SymTableBuilder.ListTypes().ShouldBeEmpty();
        
        var exception = res.Exceptions.ShouldHaveSingleItem();
        exception.Code.ShouldBe(PlampExceptionInfo.GenericParameterHasSameNameAsBuiltinMember().Code);
    }

    /// <summary>
    /// Имя generic-параметра типа не должно совпадать с именем встроенной функции.
    /// </summary>
    [Fact]
    public void GenericParameterHasSameNameAsBuiltinFunction_ReturnsException()
    {
        var code = """
                   module test;
                   type A[strLen] {}
                   """;
        
        var res = SetupAndAct(code);
        
        res.SymTableBuilder.ListTypes().ShouldBeEmpty();
        
        var exception = res.Exceptions.ShouldHaveSingleItem();
        exception.Code.ShouldBe(PlampExceptionInfo.GenericParameterHasSameNameAsBuiltinMember().Code);
    }

    /// <summary>
    /// Имена generic-параметров типа не должны совпадать.
    /// </summary>
    [Fact]
    public void TwoDuplicateGenericParameters_ReturnsException()
    {
        var code = """
                   module test;
                   type A[B, B] {}
                   """;

        var res = SetupAndAct(code);
        
        res.SymTableBuilder.ListTypes().ShouldBeEmpty();
        
        res.Exceptions.Count.ShouldBe(2);
        res.Exceptions.Select(x => x.Code).ShouldAllBe(x => x == PlampExceptionInfo.DuplicateGenericParameterName().Code);
    }

    /// <summary>
    /// Два корректных generic-параметра типа добавляются в таблицу символов.
    /// </summary>
    [Fact]
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

    /// <summary>
    /// Несколько generic-параметров с именем типа возвращают ошибки.
    /// </summary>
    [Fact]
    public void TwoGenericParamsHasSameNameAsDefiningType_ReturnsException()
    {
        var code = """
                   module test;
                   type A[A, A] {}
                   """;
        
        var res = SetupAndAct(code);
        res.SymTableBuilder.ListTypes().ShouldBeEmpty();

        res.Exceptions.ShouldAllBe(x => x.Code == PlampExceptionInfo.GenericParameterNameSameAsDefiningType().Code);
    }

    /// <summary>
    /// Несколько generic-параметров с именем встроенного типа возвращают ошибки.
    /// </summary>
    [Fact]
    public void TwoGenericParametersHasSameNameAsBuiltinType_ReturnsException()
    {
        var code = """
                   module test;
                   type A[byte, byte] {}
                   """;
        
        var res = SetupAndAct(code);
        res.SymTableBuilder.ListTypes().ShouldBeEmpty();
        
        res.Exceptions.ShouldAllBe(x => x.Code == PlampExceptionInfo.GenericParameterHasSameNameAsBuiltinMember().Code);
    }

    /// <summary>
    /// Типы с одинаковым именем и разным числом generic-параметров не добавляются в таблицу символов.
    /// </summary>
    [Fact]
    public void TwoTypesWithSameNameAndDifferentGenericCount_ReturnsNoExceptionSymTableEmpty()
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
            [weaver.WeaveDiffs]
        );
        return ctx;
    }
}
