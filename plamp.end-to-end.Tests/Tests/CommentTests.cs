using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты комментариев.
/// Помимо вызова функций, файл утыкан комментариями для проверки сборки
/// </summary>
public class CommentTests
{
    private const string SourceFile = "Comments.plp";

    /// <summary>
    /// Набор сценариев, где комментарии должны быть проигнорированы. Функция должна вернуть контрольное значение
    /// </summary>
    public static TheoryData<string, object?[], int> IntCommentScenarios => new()
    {
        { "single_line_comment_after_statement", [], 42 },
        { "multi_line_comment_between_tokens", [], 42 },
        { "multi_line_comment_inside_expression", [], 42 },
        { "nested_comment_markers_stay_inside_outer_comment", [], 42 },
        { "comments_between_function_arguments", [20, 22], 42 },
        { "cursed_abomination", [true], 42 },
        { "cursed_abomination", [false], 0 }
    };

    /// <summary>
    /// Проверяет, что маркеры комментариев внутри строкового литерала остаются частью строки
    /// </summary>
    [Fact]
    public async Task KeepsCommentMarkersInsideStringLiteral()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<string>("comment_markers_inside_string_are_not_comments");

        result.ShouldBe("/* not comment */// still text?/*\"*/");
    }

    /// <summary>
    /// Проверяет, что комментарии не влияют на выполнение функции из набора <see cref="IntCommentScenarios"/>
    /// </summary>
    /// <param name="methodName">Имя функции</param>
    /// <param name="arguments">Аргументы, передаваемые в вызываемую функцию</param>
    /// <param name="expected">Ожидаемый результат выполнения</param>
    [Theory]
    [MemberData(nameof(IntCommentScenarios))]
    public async Task CompileAndInvokeMethod_IgnoresCommentsInIntScenario(
        string methodName,
        object?[] arguments,
        int expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>(methodName, arguments);

        result.ShouldBe(expected);
    }
}
