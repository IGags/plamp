using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты с проверкой работы вложенности, ветвления и циклов
/// </summary>
public class ControlFlowTests
{
    private const string SourceFile = "ControlFlow.plp";

    /// <summary>
    /// Проверяет цепочку if с ранними return для отрицательного, нулевого и положительного значения
    /// </summary>
    [Theory]
    [InlineData(-1, "negative")]
    [InlineData(0, "zero")]
    [InlineData(1, "positive")]
    public async Task RunsIfElseChain(int value, string expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<string>("if_else_chain", value);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет while-цикл с условием <= и накоплением суммы
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 6)]
    [InlineData(8, 36)]
    public async Task RunsWhileUntilLimit(int limit, int expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("sum_until_limit", limit);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет while-цикл с накоплением произведения и поэтапным декрементом локальной переменной
    /// </summary>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(5, 120)]
    [InlineData(7, 5040)]
    public async Task RunsFactorialLoop(int value, int expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("factorial", value);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет continue внутри while: чётные значения пропускаются, нечётные учитываются
    /// </summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 3)]
    [InlineData(10, 6)]
    public async Task ContinueSkipsEvenNumbers(int limit, int expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("continue_skips_even_numbers", limit);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет break внутри бесконечного while, чтобы цикл завершался при достижении нужного значения
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(42)]
    public async Task BreakStopsLoop(int stopAt)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("break_stops_loop", stopAt);

        result.ShouldBe(stopAt);
    }

    /// <summary>
    /// Проверяет составное булево выражение с &&, || и ! на примере xor-логики
    /// </summary>
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public async Task EvaluatesBooleanExpression(bool first, bool second, bool expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<bool>("boolean_expression", first, second);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет, что continue во вложенном цикле влияет только на ближайший while и не пропускает итерацию внешнего цикла
    /// </summary>
    [Fact]
    public async Task ContinueAffectsNearestLoop()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("nested_continue_affects_nearest_loop");

        result.ShouldBe(12);
    }

    /// <summary>
    /// Проверяет, что break во вложенном цикле завершает только ближайший while, а внешний цикл продолжает работу
    /// </summary>
    [Fact]
    public async Task BreakAffectsNearestLoop()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("nested_break_affects_nearest_loop");

        result.ShouldBe(6);
    }

    /// <summary>
    /// Проверяет, что одинаковые имена переменных допустимы в непересекающихся зонах видимости
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AllowsSameVariableNameInDisjointIfBodies(bool flag)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("same_variable_name_in_disjoint_if_bodies", flag);

        result.ShouldBe(42);
    }

    /// <summary>
    /// Проверяет, что переменная из внутреннего зоны видимости не мешает переменной внешней
    /// </summary>
    [Fact]
    public async Task AllowsSameVariableNameInInnerBodyAndKeepsOuterValue()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("same_variable_name_in_inner_body_preserves_outer_value");

        result.ShouldBe(42);
    }
}
