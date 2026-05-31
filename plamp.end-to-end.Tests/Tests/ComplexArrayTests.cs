using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты массивов со сложными элементами
/// </summary>
public class ComplexArrayTests
{
    private const string SourceFile = "ComplexArrays.plp";

    /// <summary>
    /// Набор операций с массивами пользовательских и джерик структур, каждая из которых должна вернуть контрольное значение 42
    /// </summary>
    public static TheoryData<string> ComplexArrayScenarios => new()
    {
        "stores_structs_in_array",
        "mutates_struct_field_inside_array",
        "stores_generic_structs_in_array",
        "passes_array_item_between_functions"
    };

    /// <summary>
    /// Проверяет запись, мутацию и передачу элементов массивов
    /// </summary>
    [Theory]
    [MemberData(nameof(ComplexArrayScenarios))]
    public async Task HandlesComplexArrayScenario(string methodName)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>(methodName);

        result.ShouldBe(42);
    }
}
