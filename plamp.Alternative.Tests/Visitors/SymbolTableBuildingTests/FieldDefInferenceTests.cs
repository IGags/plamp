using System.Linq;
using plamp.Alternative.Visitors.SymbolTableBuilding;
using plamp.Alternative.Visitors.SymbolTableBuilding.FieldDefInference;
using plamp.Alternative.Visitors.SymbolTableBuilding.TypedefInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.SymbolTableBuildingTests;

public class FieldDefInferenceTests
{
    [Fact]
    // Пустой тип - корректно
    public void EmptyType_Correct()
    {
        var code = """
                   module test;
                   type A {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        type.Fields.ShouldBeEmpty();
    }

    [Fact]
    // Одно поле с известным рантайм типом - корректно
    public void SingleFieldWithRuntimeType_Correct()
    {
        var code = """
                   module test;
                   type A {x: int}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        var field = type.Fields.ShouldHaveSingleItem();
        field.Name.ShouldBe("x");
        field.FieldType.ShouldBe(Builtins.Int);
    }

    [Fact]
    // Одно поле с известным кастомным типом - корректно
    public void SingleFieldWithCustomType_Correct()
    {
        var code = """
                   module test;
                   type A {x: B}
                   type B {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        var types = res.SymTableBuilder.ListTypes();
        types.Count.ShouldBe(2);
        var fieldTypeInfo = types.First(x => x.Name == "B");
        fieldTypeInfo.Fields.ShouldBeEmpty();
        
        var parentTypeInfo = types.First(x => x.Name == "A");
        var field = parentTypeInfo.Fields.ShouldHaveSingleItem();
        field.Name.ShouldBe("x");
        field.FieldType.ShouldBe(fieldTypeInfo);
    }

    [Fact]
    // Одно поле с объявляющим типом - корректно
    public void SingleFieldWithEnclosingType_Correct()
    {
        var code = """
                   module test;
                   type A {x: A}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        var field = type.Fields.ShouldHaveSingleItem();
        
        field.Name.ShouldBe("x");
        field.FieldType.ShouldBe(type);
    }

    [Fact]
    // Одно поле с известным типом массива - корректно
    public void SingleFieldWithArrayType_Correct()
    {
        var code = """
                   module test;
                   type A {x: []B}
                   type B {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        
        var types = res.SymTableBuilder.ListTypes();
        //Тип массива тоже учитывается
        types.Count.ShouldBe(2);
        var fieldTypeInfo = types.First(x => x.Name == "B");
        fieldTypeInfo.Fields.ShouldBeEmpty();
        
        var parentTypeInfo = types.First(x => x.Name == "A");
        var field = parentTypeInfo.Fields.ShouldHaveSingleItem();
        field.Name.ShouldBe("x");
        field.FieldType.ShouldBe(fieldTypeInfo.MakeArrayType());
    }

    [Fact]
    // Одно поле с неизвестным типом - возврат ошибки
    public void SingleFieldWithUnknownType_ReturnsException()
    {
        var code = """
                   module test;
                   type A {x: B}
                   """;
        var res = SetupAndAct(code);
        var ex = res.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.TypeIsNotFound("B").Code);
        
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        type.Fields.ShouldBeEmpty();
    }

    [Fact]
    // Два поля с одинаковым именем - возврат ошибки
    public void TwoFieldsWithSameName_ReturnsException()
    {
        var code = """
                   module test;
                   type A {x, x: int}
                   """;
        var res = SetupAndAct(code);
        
        res.Exceptions.Count.ShouldBe(2);
        res.Exceptions.Select(x => x.Code)
            .All(x => x == PlampExceptionInfo.DuplicateFieldDefinition("x").Code)
            .ShouldBeTrue();
        
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        type.Fields.ShouldBeEmpty();
    }

    [Fact]
    // Поле имеет имя схожее с родительским типом - возврат ошибки
    public void FieldWithSameNameAsParentType_ReturnsException()
    {
        var code = """
                   module test;
                   type A {A: int}
                   """;
        var res = SetupAndAct(code);
        var ex = res.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.FieldCannotHasSameNameAsEnclosingType().Code);
     
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        type.Fields.ShouldBeEmpty();
    }
    
    private SymbolTableBuildingContext SetupAndAct(string code)
    {
        var typeDefWeaver = new TypedefInferenceWeaver();
        var fieldDefWeaver = new FieldDefInferenceWeaver();
        return CompilationPipelineBuilder.RunSymbolTableBuildingPipeline(code,
        [
            (ast, cxt) => typeDefWeaver.WeaveDiffs(ast, cxt),
            (ast, cxt) => fieldDefWeaver.WeaveDiffs(ast, cxt)
        ]);
    }
}