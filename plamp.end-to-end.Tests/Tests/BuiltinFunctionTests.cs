using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты builtin
/// </summary>
public class BuiltinFunctionTests
{
    private const string SourceFile = "Builtins.plp";

    /// <summary>
    /// Набор builtin сценариев
    /// </summary>
    public static TheoryData<string, object> BuiltinValueScenarios => new()
    {
        { "string_length_counts_escape_as_single_character", 4 },
        { "array_length_for_int_array", 42 },
        { "make_array_fills_int_values", 42 },
        { "return_int_as_any", 42 },
        { "return_string_as_any", "value" }
    };

    /// <summary>
    /// Набор сравнений через builtin функции
    /// </summary>
    public static TheoryData<string, bool> BuiltinBooleanScenarios => new()
    {
        { "string_equals_same_value", true },
        { "string_equals_different_value", false },
        { "array_equals_same_int_values", true },
        { "array_equals_different_int_values", false },
        { "array_equals_different_lengths", false },
        { "array_equals_different_element_types", false },
        { "array_equals_same_string_values", true },
        { "array_equals_nested_arrays", true },
        { "array_equals_empty_arrays", true },
        { "strings_equals_empty_strings", true }
    };

    /// <summary>
    /// Проверяет builtin функции
    /// </summary>
    [Theory]
    [MemberData(nameof(BuiltinValueScenarios))]
    public async Task ReturnsBuiltinValue(string methodName, object expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke(methodName);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет builtin функции сравнения
    /// </summary>
    [Theory]
    [MemberData(nameof(BuiltinBooleanScenarios))]
    public async Task ReturnsBuiltinBoolean(string methodName, bool expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<bool>(methodName);

        result.ShouldBe(expected);
    }
}
