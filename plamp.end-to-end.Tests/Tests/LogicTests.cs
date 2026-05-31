using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты логических операторов и логики
/// </summary>
public class LogicTests
{
    private const string SourceFile = "Logic.plp";

    /// <summary>
    /// Все комбинации трёх bool аргументов для проверки логических выражений
    /// </summary>
    public static TheoryData<bool, bool, bool> BooleanTriples
    {
        get
        {
            var data = new TheoryData<bool, bool, bool>();
            foreach (var first in new[] { false, true })
            foreach (var second in new[] { false, true })
            foreach (var third in new[] { false, true })
            {
                data.Add(first, second, third);
            }

            return data;
        }
    }

    /// <summary>
    /// Проверяет выражение со скобками, где результат зависит от группировки вокруг && и ||.
    /// </summary>
    [Theory]
    [MemberData(nameof(BooleanTriples))]
    public async Task EvaluatesParenthesizedLogic(
        bool first,
        bool second,
        bool third)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);
        var expected = (first && second) || (!first && third);

        var result = program.Invoke<bool>("parenthesized_logic", first, second, third);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет отрицание составного выражения с вложенными скобками
    /// </summary>
    [Theory]
    [MemberData(nameof(BooleanTriples))]
    public async Task EvaluatesInvertedParenthesizedLogic(
        bool first,
        bool second,
        bool third)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);
        var expected = !(first && (second || third));

        var result = program.Invoke<bool>("inverted_parenthesized_logic", first, second, third);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет использование составного bool выражения со скобками как условия if
    /// </summary>
    [Theory]
    [MemberData(nameof(BooleanTriples))]
    public async Task UsesLogicExpressionInIfBlock(
        bool first,
        bool second,
        bool third)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);
        var expected = (first && second) || third ? 42 : 0;

        var result = program.Invoke<int>("logic_in_if_block", first, second, third);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет использование bool переменной и составного bool выражения как условия while.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 3)]
    [InlineData(10, 6)]
    public async Task UsesLogicExpressionInWhileBlock(int limit, int expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("logic_in_while_block", limit);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет, что функция может возвращать составное bool выражение как результат bool типа
    /// </summary>
    [Theory]
    [InlineData(10, false)]
    [InlineData(15, true)]
    [InlineData(20, false)]
    public async Task ReturnsBoolExpression(int value, bool expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<bool>("returns_bool_type", value);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет запись составного bool выражения в новую переменную и последующий return этой переменной
    /// </summary>
    [Theory]
    [InlineData(0, 1, false)]
    [InlineData(10, 20, true)]
    [InlineData(20, 10, false)]
    public async Task StoresBoolExpressionInVariable(int first, int second, bool expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<bool>("stores_bool_expression_in_variable", first, second);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет переприсваивание bool переменной результатом нового bool выражения
    /// </summary>
    [Theory]
    [InlineData(10, 20, true)]
    [InlineData(20, 10, false)]
    [InlineData(10, 60, false)]
    public async Task ReassignsBoolExpressionVariable(int first, int second, bool expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<bool>("reassigns_bool_expression_variable", first, second);

        result.ShouldBe(expected);
    }
}
