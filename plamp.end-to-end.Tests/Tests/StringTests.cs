using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты строк
/// </summary>
public class StringTests
{
    private const string SourceFile = "Strings.plp";

    /// <summary>
    /// Набор сценариев строковой конкатенации через оператор +
    /// </summary>
    public static TheoryData<string, object?[], string> StringConcatenations => new()
    {
        { "concat_literals", [], "plamp" },
        { "concat_arguments", ["pl", "amp"], "plamp" },
        { "concat_chain", [], "abcd" },
        { "concat_string_with_char_literal", [], "AZ" },
        { "concat_char_literal_with_string", [], "AZ" },
        { "concat_string_with_char_variable", [], "plamp" },
        { "concat_string_with_char_from_function", [], "plamp" }
    };

    /// <summary>
    /// Проверяет конкатенацию строковых литералов, аргументов функции, цепочек сложений и char операндов
    /// </summary>
    [Theory]
    [MemberData(nameof(StringConcatenations))]
    public async Task ConcatStringsTests(
        string methodName,
        object?[] arguments,
        string expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<string>(methodName, arguments);

        result.ShouldBe(expected);
    }
}
