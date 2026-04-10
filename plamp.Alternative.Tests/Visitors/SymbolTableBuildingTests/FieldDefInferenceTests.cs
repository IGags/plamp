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

    [Fact]
    // У поля не только неверный тип, но и некорректное имя. Обе ошибки должны быть подсвечены
    public void InferenceFieldWithUnknownTypeAndSameNameAsDefiningType_ReturnsBothException()
    {
        var code = """
                   module test;
                   type A {A: IMNOTAVALIDTYPE}
                   """;
        
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(2);
     
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        type.Fields.ShouldBeEmpty();
        
        var errorCodesShould = new []
        {
            PlampExceptionInfo.FieldCannotHasSameNameAsEnclosingType().Code, 
            PlampExceptionInfo.TypeIsNotFound("").Code
        };
        var errorCodesActual = res.Exceptions.Select(x => x.Code);
        
        errorCodesShould.ShouldAllBe(x => errorCodesActual.Contains(x));
    }

    [Fact]
    // 2 поля имеют одинаковое имя, равное имени объявляющего типа. Вернётся только ошибка о том, что их имя схоже с именем объявляющего типа
    public void InferenceFieldsWithDuplicateNamesSameAsDefiningType_ReturnsSameAsDefiningTypeException()
    {
        var code = """
                   module test;
                   type A {A, A: ulong}
                   """;
        
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(2);
        
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        type.Fields.ShouldBeEmpty();
        
        var errorCodesShould = new []
        {
            PlampExceptionInfo.FieldCannotHasSameNameAsEnclosingType().Code, 
            PlampExceptionInfo.FieldCannotHasSameNameAsEnclosingType().Code
        };
        var errorCodesActual = res.Exceptions.Select(x => x.Code);
        
        errorCodesShould.ShouldAllBe(x => errorCodesActual.Contains(x));
    }

    [Fact]
    // У поля имя совпадает с одним из встроенных типов.
    public void FieldHasSameNameAsBuiltinType_ReturnsException()
    {
        var code = """
                   module test;
                   type A { bool: int }
                   """;
        
        var res = SetupAndAct(code);
        var ex =  res.Exceptions.ShouldHaveSingleItem();
        
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        type.Fields.ShouldBeEmpty();
        
        ex.Code.ShouldBe(PlampExceptionInfo.FieldHasSameNameAsBuiltinType().Code);
    }

    [Fact]
    // У двух полей имя совпадает с именем встроенного типа, будет возвращена только эта ошибка
    public void DuplicateFieldsWithBuiltinTypeName_ReturnsSameAsBuiltinTypeException()
    {
        var code = """
                   module test;
                   type A { string, string: ushort }
                   """;

        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(2);
        
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        type.Fields.ShouldBeEmpty();
        
        var errorCodesShould = new []
        {
            PlampExceptionInfo.FieldHasSameNameAsBuiltinType().Code, 
            PlampExceptionInfo.FieldHasSameNameAsBuiltinType().Code
        };
        var errorCodesActual = res.Exceptions.Select(x => x.Code);
        
        errorCodesShould.ShouldAllBe(x => errorCodesActual.Contains(x));
    }

    [Fact]
    // Корректное поле с типом дженерик параметра
    public void FieldWithGenericType_Correct()
    {
        var code = """
                   module test;
                   type A[B] { fld: B }
                   """;
        
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        var fld = type.Fields.Single();
        
        fld.Name.ShouldBe("fld");

        var fldType = fld.FieldType;
        
        fldType.IsGenericTypeParameter.ShouldBeTrue();
        fldType.Name.ShouldBe("B");
    }

    [Fact]
    //Корректное поле с именем равным имени типа дженерик параметра
    public void FieldHasSameNameAsGenericType_Correct()
    {
        var code = """
                   module test;
                   type A[B] { B: B }
                   """;

        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        var fld = type.Fields.Single();
        
        fld.Name.ShouldBe("B");
        
        var fldType = fld.FieldType;
        fldType.IsGenericTypeParameter.ShouldBeTrue();
        fldType.Name.ShouldBe("B");
        
        type.GetGenericParameters().ShouldContain(fldType);
    }

    [Fact]
    //У поля массив дженерик параметров
    public void FieldHasGenericParameterArrayType_Correct()
    {
        var code = """
                   module test;
                   type A[T] { B: []T }
                   """;

        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        
        var type = res.SymTableBuilder.ListTypes().ShouldHaveSingleItem();
        var fld = type.Fields.Single();
        
        fld.Name.ShouldBe("B");
        
        var fldType = fld.FieldType;
        fldType.IsArrayType.ShouldBeTrue();
        
        var elemType = fldType.ElementType().ShouldNotBeNull();
        
        elemType.Name.ShouldBe("T");
        elemType.IsArrayType.ShouldBeFalse();
        elemType.IsGenericTypeParameter.ShouldBeTrue();
        
        type.GetGenericParameters().ShouldContain(elemType);
    }

    [Fact]
    // Параметр используется как аргумент в другом дженерик типе.
    public void GenericParameterAsArgumentOfAnotherGenericType_Correct()
    {
        var code = """
                   module test;
                   type T1[G]{}
                   type T2[T]{ fld: T1[T] }
                   """;
        
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        
        var type = res.SymTableBuilder.ListTypes().Single(x => x.DefinitionName == "T2");
        var fld = type.Fields.ShouldHaveSingleItem();
        
        fld.Name.ShouldBe("fld");
        
        var fldType = fld.FieldType;
        fldType.IsGenericType.ShouldBeTrue();
        var arguments = fldType.GetGenericArguments();
        var genericArg = arguments.ShouldHaveSingleItem();
        
        genericArg.Name.ShouldBe("T");
        genericArg.IsGenericTypeParameter.ShouldBeTrue();
        
        type.GetGenericParameters().ShouldContain(genericArg);
    }

    [Fact]
    //Если в типе есть дженерик параметр и в модуле есть одноимённый тип, то для типа поля будет выбран тип параметра
    public void GenericParameterHasHigherPriorityThanTypeDefinedInModule_Correct()
    {
        var code = """
                   module test;
                   type T {}
                   type G[T] { fld: T }
                   """;

        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
        
        var type = res.SymTableBuilder.ListTypes().Single(x => x.DefinitionName == "G");
        var fld = type.Fields.Single();
        
        fld.Name.ShouldBe("fld");
        
        var fldType = fld.FieldType;
        fldType.IsGenericTypeParameter.ShouldBeTrue();
        fldType.Name.ShouldBe("T");
        
        type.GetGenericParameters().ShouldContain(fldType);
    }

    [Fact]
    //Дженерик тип со вложенным дженериком
    public void FieldHasComplexGenericTypeWithNesting_Correct()
    {
        var code = """
                   module test;
                   type Ls[T] { inner: T }
                   type Fin[T] { inner: Ls[Ls[Ls[T]]] }
                   """;
        
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();

        var types = res.SymTableBuilder.ListTypes();
        types.Count.ShouldBe(2);

        var finType = types.Single(x => x.DefinitionName == "Fin");
        var fld = finType.Fields.ShouldHaveSingleItem();
        var lsType = types.Single(x => x.DefinitionName == "Ls");

        var genericParam = finType.GenericParameterBuilders.ShouldHaveSingleItem();
        var typeShould = lsType.MakeGenericType(
            [
                lsType.MakeGenericType(
                    [
                        lsType.MakeGenericType([genericParam]).ShouldNotBeNull()
                    ]).ShouldNotBeNull()
            ]).ShouldNotBeNull();
        
        fld.Name.ShouldBe("inner");
        fld.FieldType.ShouldBe(typeShould);
    }
    
    private SymbolTableBuildingContext SetupAndAct(string code)
    {
        var typeDefWeaver = new TypedefInferenceWeaver();
        var fieldDefWeaver = new FieldDefInferenceWeaver();
        var (ctx, _) = CompilationPipelineBuilder.RunSymTableVisitors(code,
        [
            (ast, cxt) => typeDefWeaver.WeaveDiffs(ast, cxt),
            (ast, cxt) => fieldDefWeaver.WeaveDiffs(ast, cxt)
        ]);
        return ctx;
    }
}