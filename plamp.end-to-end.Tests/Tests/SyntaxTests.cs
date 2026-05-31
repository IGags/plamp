using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// E2e-тесты синтаксиса: объявления функций и структур, grouped declarations, multi-assignment,
/// пустые структуры, создание структур и type initializer-ы.
/// </summary>
public class SyntaxTests
{
    private const string SourceFile = "Syntax.plp";

    /// <summary>
    /// Набор синтаксических сценариев, возвращающих int.
    /// </summary>
    public static TheoryData<string, object[], int> IntSyntaxScenarios => new()
    {
        { "constant_function", [], 42 },
        { "function_with_many_arguments", [10, 20, 12], 42 },
        { "grouped_field_types_and_multi_assignment", [], 42 },
        { "structure_field_roundtrip", [], 42 },
        { "structure_variable_copy_keeps_original_value", [], 42 },
        { "return_new_object_without_variable", [], 42 },
        { "swaps_variables_with_multi_assignment", [], 42 },
        { "assigns_fields_and_indexers_with_multi_assignment", [], 42 },
        { "grouped_assignment_of_empty_structs", [], 42 },
        { "default_init_empty_struct_without_initializer", [], 42 },
        { "use_empty_struct_array_roundtrip", [], 42 },
        { "initializes_int_fields", [], 42 },
        { "initializes_fields_from_function_calls", [], 42 },
        { "initializes_generic_struct_field", [], 42 },
        { "if_else_returns_on_all_paths", [false], 2}
    };

    /// <summary>
    /// Набор синтаксических сценариев, возвращающих string.
    /// </summary>
    public static TheoryData<string, string> StringSyntaxScenarios => new()
    {
        { "initializes_string_and_int_fields", "value" },
        { "initializes_generic_pair_and_returns_string_field", "value" }
    };

    /// <summary>
    /// Проверяет синтаксические сценарии, где результатом является int.
    /// </summary>
    [Theory]
    [MemberData(nameof(IntSyntaxScenarios))]
    public async Task ReturnsIntFromSyntaxScenario(string methodName, object[] arguments, int expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>(methodName, arguments);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет синтаксические сценарии, где результатом является string.
    /// </summary>
    [Theory]
    [MemberData(nameof(StringSyntaxScenarios))]
    public async Task ReturnsStringFromSyntaxScenario(string methodName, string expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<string>(methodName);

        result.ShouldBe(expected);
    }
}
