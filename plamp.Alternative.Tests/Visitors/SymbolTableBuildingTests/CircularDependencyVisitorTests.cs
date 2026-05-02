using System.Linq;
using plamp.Alternative.Visitors.SymbolTableBuilding;
using plamp.Alternative.Visitors.SymbolTableBuilding.CircularDependency;
using plamp.Alternative.Visitors.SymbolTableBuilding.FieldDefInference;
using plamp.Alternative.Visitors.SymbolTableBuilding.TypedefInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.SymbolTableBuildingTests;

/// <summary>
/// Тесты валидатора, проверяющего наличие циклических ссылок внутри типа.
/// </summary>
public class CircularDependencyVisitorTests
{
    /// <summary>
    /// Тип и несколько полей - корректно
    /// </summary>
    [Fact]
    public void TypeWithNotRecursiveFields_Correct()
    {
        const string code = """
                            type A {
                                a: float;
                                b: B;
                            }
                            
                            type B {
                                a: string;
                            }
                            """;

        var ctx = SetupAndAct(code);
        
        ctx.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    public void TypeWithSameTypeField_Exception()
    {
        const string code = """
                            type A {
                                a: A;
                            }
                            """;
        var ctx = SetupAndAct(code);

        var exception = ctx.Exceptions.ShouldHaveSingleItem();
        exception.Code.ShouldBe(PlampExceptionInfo.FieldProduceCircularDependency().Code);
    }

    /// <summary>
    /// Тип1 и Тип2 образуют цикл - ошибка
    /// </summary>
    [Fact]
    public void TwoTypesHasReferenceToEachOther_CreateExceptionForBothOfThem()
    {
        const string code = """
                            type A { b: B; }
                            type B { a: A; }
                            """;
        var ctx = SetupAndAct(code);

        var errors = ctx.Exceptions;
        errors.Count.ShouldBe(2);
        errors.Select(x => x.Code).ShouldAllBe(x => x.Equals(PlampExceptionInfo.FieldProduceCircularDependency().Code));
    }

    /// <summary>
    /// Дженерик тип - корректно
    /// </summary>
    [Fact]
    public void SimpleGenericType_Correct()
    {
        const string code = """
                            type Type[T] {
                                f1: T;
                                f2: []T;
                            }
                            """;
        var ctx = SetupAndAct(code);
        ctx.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Два дженерик типа, вложенный полностью имплементирован - корректно
    /// </summary>
    [Fact]
    public void GenericHasFullyImplementedGenericField_Correct()
    {
        const string code = """
                            type A[T1] {
                                fld: T1;
                            }
                            
                            type B[T1] {
                                fld1: T1;
                                fld2: A[int];
                            }
                            """;
        
        var ctx = SetupAndAct(code);
        ctx.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Два дженерик типа, вложенный использует параметр внешнего - корректно
    /// </summary>
    [Fact]
    public void GenericTypeHasGenericTypeFieldThatUsesParentGenericParameter_Correct()
    {
        const string code = """
                            type A[T1] {
                                fld: T1;
                            }
                            
                            type B[T1] {
                                fld1: A[T1];
                            }
                            """;
        
        var ctx = SetupAndAct(code);
        ctx.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// У дженерика поле того же типа
    /// </summary>
    [Fact]
    public void GenericHasFieldWithSameType_Error()
    {
        const string code = """
                            type Gen[T] { f: Gen[T]; }
                            """;
        
        var ctx = SetupAndAct(code);
        
        var exception = ctx.Exceptions.ShouldHaveSingleItem();
        exception.Code.ShouldBe(PlampExceptionInfo.FieldProduceCircularDependency().Code);
    }

    /// <summary>
    /// Дженерик тип имеет поле того же типа, только имплементированное - ошибка
    /// </summary>
    [Fact]
    public void GenericTypeHasFieldWithSameTypeImplemented_Error()
    {
        const string code = """
                            type A[T] { f : A[int]}
                            """;
        
        var ctx = SetupAndAct(code);
        var exception = ctx.Exceptions.ShouldHaveSingleItem();
        exception.Code.ShouldBe(PlampExceptionInfo.FieldProduceCircularDependency().Code);
    }

    /// <summary>
    /// Два дженеирк типа, вложенный использует параметр внешнего и имеет поле типа внешнего. - ошибка
    /// </summary>
    [Fact]
    public void GenericTypeHasOtherGenericTypeFieldWithFirstTypeFieldUsesParam_Error()
    {
        const string code = """
                            type A[T] { f: B[T] }
                            type B[T] { f: A[T] }
                            """;
        var ctx = SetupAndAct(code);

        ctx.Exceptions.Count.ShouldBe(2);
        ctx.Exceptions.Select(x => x.Code).ShouldAllBe(x => x.Equals(PlampExceptionInfo.FieldProduceCircularDependency().Code));
    }

    /// <summary>
    /// Два дженеирк типа, вложенный использует параметр внешнего и имеет частично имплементированный внешний - ошибка 
    /// </summary>
    [Fact]
    public void GenericTypeHasOtherGenericTypeFieldWithFirstTypeFieldImplementedPartially_Error()
    {
        const string code = """
                            type Map[TK, TV] { Keys: MapList[TK]; Vals: MapList[TV] }
                            type MapList[T] { m: Map[T, int] }
                            """;
        
        var ctx = SetupAndAct(code);

        ctx.Exceptions.Count.ShouldBe(3);
        ctx.Exceptions.Select(x => x.Code).ShouldAllBe(x => x.Equals(PlampExceptionInfo.FieldProduceCircularDependency().Code));
    }

    /// <summary>
    /// По-умолчанию типы массивов с элементом исходного типа не разрешены для однозначного поведения.
    /// Так как в теории при инициализации типа может быть разрешено иметь массив нулевой длины, но на практике это создаст угловые случаи и усложнит код.   
    /// </summary>
    [Fact]
    public void TypeHasArrayWithSameType_Error()
    {
        const string code = """
                            type B {D: []B}
                            """;
        var ctx = SetupAndAct(code);
        var exception = ctx.Exceptions.ShouldHaveSingleItem();
        exception.Code.ShouldBe(PlampExceptionInfo.FieldProduceCircularDependency().Code);
    }

    /// <summary>
    /// У второго типа первый как массив, а у первого второй как значение
    /// </summary>
    [Fact]
    public void TypeHasOtherTypeAsArrayRecursiveField_Error()
    {
        const string code = """
                            type A {B: B}
                            type B {A: []A}
                            """;
        var ctx = SetupAndAct(code);
        ctx.Exceptions.Count.ShouldBe(2);
        ctx.Exceptions.Select(x => x.Code).ShouldAllBe(x => x.Equals(PlampExceptionInfo.FieldProduceCircularDependency().Code));
    }

    /// <summary>
    /// Первый и второй типы имеют ссылки-массивы друг на друга
    /// </summary>
    [Fact]
    public void TypesHasArrayReferenceToEachOther_Error()
    {
        const string code = """
                            type A {B: []B}
                            type B {A: []A}
                            """;
        var ctx = SetupAndAct(code);
        ctx.Exceptions.Count.ShouldBe(2);
        ctx.Exceptions.Select(x => x.Code).ShouldAllBe(x => x.Equals(PlampExceptionInfo.FieldProduceCircularDependency().Code));
    }

    [Fact]
    public void TypeHasArrayAndReferenceOnOtherGenericThatHasReferenceOnFirstGeneric_Error()
    {
        const string code = """
                            type A[T1] {B: []B[T1]}
                            type B[T2] {A: []A[T2]}
                            """;
        var ctx = SetupAndAct(code);
        ctx.Exceptions.Count.ShouldBe(2);
        ctx.Exceptions.Select(x => x.Code).ShouldAllBe(x => x.Equals(PlampExceptionInfo.FieldProduceCircularDependency().Code));
    }
    
    private SymbolTableBuildingContext SetupAndAct(string code)
    {
        var typeInf = new TypedefInferenceWeaver();
        var fieldInf = new FieldDefInferenceWeaver();
        var circularWeaver = new TypeHasCircularDependencyValidator();
        var (ctx, _) = CompilationPipelineBuilder.RunSymTableVisitors(
            code,
            [
                (ast, ctx) => typeInf.WeaveDiffs(ast, ctx),
                (ast, ctx) => fieldInf.WeaveDiffs(ast, ctx),
                (ast, ctx) => circularWeaver.Validate(ast, ctx),
            ]
        );
        return ctx;
    }
}