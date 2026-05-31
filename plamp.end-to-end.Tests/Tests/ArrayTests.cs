using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты массивов
/// </summary>
public class ArrayTests
{
    private const string SourceFile = "Arrays.plp";

    /// <summary>
    /// Набор операций с массивами, которые должны вернуть контрольное значение 42
    /// </summary>
    public static TheoryData<string> ArrayScenarios => new()
    {
        "get_array_element_by_literal_index",
        "set_array_element_by_expression_index",
        "nested_array_index_as_indexer",
        "array_item_increment_roundtrip",
        "array_of_arrays_roundtrip",
        "array_of_arrays_inner_reassignment",
        "byte_indexer_is_accepted"
    };

    /// <summary>
    /// Проверяет запись, чтение, индексы, массивы массивов и byte-индексатор
    /// </summary>
    [Theory]
    [MemberData(nameof(ArrayScenarios))]
    public async Task RunsArrayScenario(string methodName)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>(methodName);

        result.ShouldBe(42);
    }

    /// <summary>
    /// Проверяет чтение элемента массива по индексу, переданному аргументом функции
    /// </summary>
    [Theory]
    [InlineData(0, 11)]
    [InlineData(1, 42)]
    [InlineData(2, 99)]
    public async Task GetsArrayElementByVariableIndex(int index, int expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("get_array_element_by_variable_index", index);

        result.ShouldBe(expected);
    }
}
