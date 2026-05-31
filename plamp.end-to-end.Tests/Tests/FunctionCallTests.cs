using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты вызова функций
/// </summary>
public class FunctionCallTests
{
    private const string SourceFile = "FunctionCalls.plp";

    /// <summary>
    /// Набор базовых сценариев вызова функций с передаваемыми аргументами и ожидаемым значением
    /// </summary>
    public static TheoryData<string, object[], int> IntFunctionCallScenarios => new()
    {
        { "call_function_without_arguments", [], 42 },
        { "pass_int_arguments_to_function", [17, 19], 36 },
        { "pass_result_to_next_function", [23], 48 },
        { "call_function_result_inside_argument", [18], 42 },
        { "recursive_sum_to_zero", [8], 36 },
        { "recursive_multiply", [6, 7], 42 },
        { "recursive_result_inside_argument", [5], 27 },
        { "call_chain", [39], 42 }
    };

    /// <summary>
    /// Проверяет базовые вызовы функций: без аргументов, с аргументами, с передачей результата в следующий вызов,
    /// с вложенным вызовом в аргументе, с рекурсией и с цепочкой вызовов.
    /// </summary>
    [Theory]
    [MemberData(nameof(IntFunctionCallScenarios))]
    public async Task ReturnsIntFromFunctionCallScenario(string methodName, object[] arguments, int expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>(methodName, arguments);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет передачу и изменение значимого типа. Исходное значение не должно меняться 
    /// </summary>
    [Fact]
    public async Task DoesNotMutateOriginalIntPassedToFunction()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("pass_int_by_value_does_not_mutate_original");

        result.ShouldBe(37);
    }

    /// <summary>
    /// Проверяет передачу и изменение ссылочного типа. Исходное значение должно измениться
    /// </summary>
    [Fact]
    public async Task MutatesArrayPassedToFunction()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("pass_array_reference_to_function");

        result.ShouldBe(42);
    }

    /// <summary>
    /// Проверяет передачу структуры в функцию для чтения значения её поля
    /// </summary>
    [Fact]
    public async Task PassesStructureValueToFunction()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("pass_structure_value_to_function");

        result.ShouldBe(42);
    }

    /// <summary>
    /// Проверяет мутацию структур. Они значимые, поэтому исходно значение не должно меняться
    /// </summary>
    [Fact]
    public async Task DoesNotMutateOriginalStructurePassedToFunction()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("pass_structure_by_value_does_not_mutate_original");

        result.ShouldBe(42);
    }
}
