using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты дженериков
/// </summary>
public class GenericTests
{
    private const string SourceFile = "Generics.plp";

    /// <summary>
    /// Набор сценариев с дженериками
    /// </summary>
    public static TheoryData<string, object[], int> GenericIntScenarios => new()
    {
        { "assign_generic_int_field", [31], 31 },
        { "reassign_generic_struct_value", [], 42 },
        { "generic_identity_int", [37], 37 },
        { "generic_second_int", [20, 31], 31 },
        { "pass_generic_struct_between_functions", [29], 29 },
        { "generic_argument_permutation_keeps_right_value", [], 42 },
        { "nested_generic_usage", [], 42 },
        { "pass_generic_function_result_between_functions", [53], 53 }
    };

    /// <summary>
    /// Набор generic сценариев, возвращающих строковое контрольное значение
    /// </summary>
    public static TheoryData<string, object[], string> GenericStringScenarios => new()
    {
        { "assign_generic_string_field", ["value"], "value" },
        { "generic_identity_string", ["text"], "text" },
        { "generic_identity_inferred_string", ["inferred"], "inferred" },
        { "generic_argument_permutation_keeps_left_value", [], "value" }
    };

    /// <summary>
    /// Проверяет generic структуры и generic функции, где результатом должен быть int
    /// </summary>
    [Theory]
    [MemberData(nameof(GenericIntScenarios))]
    public async Task ReturnsGenericIntValue(string methodName, object[] arguments, int expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>(methodName, arguments);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет generic структуры и generic функции, где результатом должен быть string
    /// </summary>
    [Theory]
    [MemberData(nameof(GenericStringScenarios))]
    public async Task ReturnsGenericStringValue(string methodName, object[] arguments, string expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<string>(methodName, arguments);

        result.ShouldBe(expected);
    }
}
