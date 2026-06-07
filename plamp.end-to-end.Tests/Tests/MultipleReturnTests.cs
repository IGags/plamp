using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Проверяет множественный возврат значений через физические CLR out-параметры.
/// </summary>
public class MultipleReturnTests
{
    private const string SourceFile = "MultipleReturns.plp";

    /// <summary>
    /// Проверяет сохранение порядка результатов при присваивании.
    /// </summary>
    [Fact]
    public async Task AssignsMultipleResultsInDeclaredOrder()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<int>("read_first_result").ShouldBe(17);
        program.Invoke<string>("read_second_result").ShouldBe("result");
    }

    /// <summary>
    /// Проверяет, что множественный возврат не ограничен арностью CLR ValueTuple.
    /// </summary>
    [Fact]
    public async Task ReturnsMoreThanEightValues()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<int>("read_ninth_result").ShouldBe(9);
    }

    /// <summary>
    /// Проверяет множественный возврат из generic-функции.
    /// </summary>
    [Fact]
    public async Task ReturnsMultipleValuesFromGenericFunction()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<int>("read_generic_results").ShouldBe(42);
    }

    /// <summary>
    /// Проверяет вывод generic-типа для функции с множественным результатом.
    /// </summary>
    [Fact]
    public async Task ReturnsMultipleValuesFromInferredGenericFunction()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<string>("read_inferred_generic_results").ShouldBe("leftright");
    }

    /// <summary>
    /// Проверяет множественный возврат из разных ветвей функции.
    /// </summary>
    [Theory]
    [InlineData(true, "positive")]
    [InlineData(false, "negative")]
    public async Task ReturnsMultipleValuesFromBranch(bool positive, string expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<string>("read_branch_pair", positive).ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет присваивание результатов в поля и элементы массивов.
    /// </summary>
    [Fact]
    public async Task AssignsMultipleResultsToComplexTargets()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<int>("assign_results_to_fields_and_indexer").ShouldBe(60);
    }

    /// <summary>
    /// Проверяет возврат ссылочного типа и структуры одним вызовом.
    /// </summary>
    [Fact]
    public async Task ReturnsReferenceAndStructureValues()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<int>("read_reference_results").ShouldBe(84);
    }

    /// <summary>
    /// Проверяет структуру в out-результате и массив в CLR return.
    /// </summary>
    [Fact]
    public async Task ReturnsStructureBeforeReferenceValue()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<int>("read_reversed_reference_results").ShouldBe(100);
    }

    /// <summary>
    /// Проверяет порядок вычисления выражений в инструкции return.
    /// </summary>
    [Fact]
    public async Task EvaluatesReturnExpressionsFromLeftToRight()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<int>("verify_return_expression_order").ShouldBe(355);
    }

    /// <summary>
    /// Проверяет несколько последовательных много-результатных вызовов.
    /// </summary>
    [Fact]
    public async Task KeepsResultsOfSequentialCallsIndependent()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<int>("use_multiple_calls").ShouldBe(28);
    }

    /// <summary>
    /// Проверяет неявное приведение int-выражений к long, float и double при множественном возврате.
    /// </summary>
    [Fact]
    public async Task ImplicitlyCastsMultipleNumericReturnValues()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<long>("read_implicitly_cast_long").ShouldBe(89L);
        program.Invoke<float>("read_implicitly_cast_float").ShouldBe(97f);
        program.Invoke<double>("read_implicitly_cast_double").ShouldBe(101d);
    }

    /// <summary>
    /// Проверяет неявное приведение int-результатов при присваивании в ранее объявленные long, float и double.
    /// </summary>
    [Fact]
    public async Task ImplicitlyCastsMultipleResultsToExistingVariables()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<double>("assign_integer_results_to_wider_types").ShouldBe(319d);
    }

    /// <summary>
    /// Проверяет return со значением и вызовом другой функции
    /// </summary>
    [Fact]
    public async Task ReturnsFunctionCallAlongsideOtherValues()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<int>("read_literal_and_call_results").ShouldBe(240);
    }

    /// <summary>
    /// Проверяет проброс множественного результата вызова через return
    /// </summary>
    [Fact]
    public async Task ForwardsMultipleResultsFromAnotherFunction()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<string>("read_forwarded_pair").ShouldBe("result");
    }

    /// <summary>
    /// Проверяет несколько вызовов функций внутри одного return
    /// </summary>
    [Fact]
    public async Task ReturnsMultipleFunctionCalls()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<int>("read_multiple_calls").ShouldBe(425);
    }

    /// <summary>
    /// Проверяет рекурсивную функцию с множественным результатом
    /// </summary>
    [Fact]
    public async Task ReturnsMultipleValuesRecursively()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<int>("read_recursive_pair").ShouldBe(156);
    }

    /// <summary>
    /// Проверяет проброс массива и структуры через множественный return
    /// </summary>
    [Fact]
    public async Task ForwardsReferenceAndStructureResults()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        program.Invoke<int>("read_forwarded_reference_results").ShouldBe(84);
    }

    /// <summary>
    /// Проверяет возврат результата функции с множественным возвратом и значения
    /// </summary>
    [Fact]
    public async Task ReturnWithMultipleReturnFromFunction()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);
        program.Invoke<int>("read_literal_and_pair_results").ShouldBe(236);
    }
    /// <summary>
    /// Проверяет физическую CLR-сигнатуру функции с множественным результатом.
    /// </summary>
    [Fact]
    public async Task EmitsEarlierResultsAsTrailingOutParameters()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);
        var method = program.GetMethod("pair");
        var parameters = method.GetParameters();
        object?[] arguments = [17, null];

        method.ReturnType.ShouldBe(typeof(string));
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(int));
        parameters[0].IsOut.ShouldBeFalse();
        parameters[1].ParameterType.ShouldBe(typeof(int).MakeByRefType());
        parameters[1].IsOut.ShouldBeTrue();
        program.Invoke("pair", arguments).ShouldBe("result");
        arguments[1].ShouldBe(17);
    }
}
