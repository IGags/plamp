using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты char
/// </summary>
public class CharTests
{
    private const string SourceFile = "Chars.plp";

    /// <summary>
    /// Набор сценариев, возвращающих конкретное значение
    /// </summary>
    public static TheoryData<string, object?[], char> CharValueScenarios => new()
    {
        { "return_plain_char_literal", [], 'a' },
        { "pass_char_between_functions", ['p'], 'p' },
        { "assign_char_to_struct_field", [], 'z' },
        { "assign_char_to_generic_field", [], 'q' },
        { "char_array_roundtrip", [], 'y' },
        { "default_char_value", [], '\0' }
    };

    /// <summary>
    /// Проверяет поддерживаемые escape последовательности char
    /// </summary>
    [Theory]
    [InlineData("return_tab_escape_char_literal", '\t')]
    [InlineData("return_new_line_escape_char_literal", '\n')]
    [InlineData("return_quote_escape_char_literal", '\'')]
    [InlineData("return_backslash_escape_char_literal", '\\')]
    public async Task ReturnsEscapedCharLiteral(string methodName, char expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<char>(methodName);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет char литералы, передачу char между функциями, поля структур, generic-поля, массивы и default-инициализацию
    /// </summary>
    [Theory]
    [MemberData(nameof(CharValueScenarios))]
    public async Task ReturnsCharValue(
        string methodName,
        object?[] arguments,
        char expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<char>(methodName, arguments);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет операторы равенства и неравенства для char значений
    /// </summary>
    [Theory]
    [InlineData("compare_equal_chars")]
    [InlineData("compare_different_chars")]
    public async Task ComparesChars(string methodName)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<bool>(methodName);

        result.ShouldBeTrue();
    }

    /// <summary>
    /// Проверяет boxing char значения в any при возврате из функции
    /// </summary>
    [Fact]
    public async Task ReturnsCharAsAny()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<object>("return_char_as_any");

        result.ShouldBe('k');
    }
}
