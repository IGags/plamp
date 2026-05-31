using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты простых математических операций
/// </summary>
public class MathOperatorsTests
{
    private const string SourceFile = "MathOperators.plp";

    /// <summary>
    /// Набор сценариев, где результат зависит от приоритета арифметических операторов и скобок
    /// </summary>
    public static TheoryData<string, int, int> IntPriorityScenarios => new()
    {
        { "expression_priority", 10, 14 },
        { "parenthesized_expression_priority", 19, 42 },
        { "nested_parenthesized_expression_priority", 16, 42 }
    };

    /// <summary>
    /// Набор операций над byte/short/uint/ulong/ushort/sbyte, которые должны сохранить корректный тип и значение
    /// </summary>
    public static TheoryData<string, object?[], object> SmallAndUnsignedNumberScenarios => new()
    {
        { "add_byte_values", [(byte)20, (byte)22], 42 },
        { "increment_byte_variable", [(byte)41], 42 },
        { "add_short_values", [(short)20, (short)22], 42 },
        { "increment_short_field", [(short)41], 42 },
        { "grouped_small_number_fields_and_assignment", [(short)20, (short)21], 42 },
        { "add_uint_values", [20u, 22u], 42u },
        { "add_ulong_values", [20ul, 22ul], 42ul },
        { "increment_ushort_field", [(ushort)41], 42 },
        { "decrement_sbyte_field", [(sbyte)43], 42 }
    };

    /// <summary>
    /// Проверяет, что базовые бинарные арифметические операторы компилируются и выполняются
    /// </summary>
    [Theory]
    [InlineData("add", 20, 22, 42)]
    [InlineData("subtract", 20, 22, -2)]
    [InlineData("multiply", 6, 7, 42)]
    [InlineData("divide", 84, 2, 42)]
    [InlineData("modulo", 85, 43, 42)]
    public async Task ReturnsBinaryArithmeticResult(
        string methodName,
        int first,
        int second,
        int expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>(methodName, first, second);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет, что арифметика над float проходит и возвращает результат с ожидаемой точностью
    /// </summary>
    [Theory]
    [InlineData("add_float", 20.5f, 21.25f, 41.75f)]
    [InlineData("subtract_float", 20.5f, 21.25f, -0.75f)]
    [InlineData("multiply_float", 6.5f, 4.0f, 26.0f)]
    [InlineData("divide_float", 21.0f, 2.0f, 10.5f)]
    public async Task ReturnsFloatArithmeticResult(
        string methodName,
        float first,
        float second,
        float expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<float>(methodName, first, second);

        Math.Abs(result - expected).ShouldBeLessThan(0.0001f);
    }

    /// <summary>
    /// Проверяет, что арифметика над double проходит и возвращает результат с ожидаемой точностью
    /// </summary>
    [Theory]
    [InlineData("add_double", 20.5d, 21.25d, 41.75d)]
    [InlineData("subtract_double", 20.5d, 21.25d, -0.75d)]
    [InlineData("multiply_double", 6.5d, 4.0d, 26.0d)]
    [InlineData("divide_double", 21.0d, 2.0d, 10.5d)]
    public async Task ReturnsDoubleArithmeticResult(
        string methodName,
        double first,
        double second,
        double expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<double>(methodName, first, second);

        Math.Abs(result - expected).ShouldBeLessThan(0.0000001d);
    }

    /// <summary>
    /// Проверяет арифметику, инкременты, декременты для малых и беззнаковых числовых типов
    /// </summary>
    [Theory]
    [MemberData(nameof(SmallAndUnsignedNumberScenarios))]
    public async Task RunsSmallOrUnsignedNumberScenario(
        string methodName,
        object?[] arguments,
        object expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke(methodName, arguments);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет, что int можно передавать в функцию с параметрами float
    /// </summary>
    [Theory]
    [InlineData("pass_int_literals_to_float_parameter")]
    [InlineData("pass_int_variables_to_float_parameter")]
    public async Task AllowsIntArgumentsForFloatParameters(string methodName)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<float>(methodName);

        Math.Abs(result - 42f).ShouldBeLessThan(0.0001f);
    }

    /// <summary>
    /// Проверяет, что int можно передавать в функцию с параметрами double
    /// </summary>
    [Theory]
    [InlineData("pass_int_literals_to_double_parameter")]
    [InlineData("pass_int_variables_to_double_parameter")]
    public async Task AllowsIntArgumentsForDoubleParameters(string methodName)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<double>(methodName);

        Math.Abs(result - 42d).ShouldBeLessThan(0.0000001d);
    }

    /// <summary>
    /// Проверяет, что выражения с несколькими арифметическими операторами сохраняют ожидаемый приоритет после парсинга
    /// </summary>
    [Theory]
    [MemberData(nameof(IntPriorityScenarios))]
    public async Task RespectsIntArithmeticPriority(
        string methodName,
        int argument,
        int expected)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>(methodName, argument);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Проверяет скобки в арифметике float, включая операции сложения, вычитания и умножения в одном выражении
    /// </summary>
    [Fact]
    public async Task RespectsParenthesizedFloatArithmeticPriority()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<float>("parenthesized_float_expression", 12.5f);

        Math.Abs(result - 42f).ShouldBeLessThan(0.0001f);
    }

    /// <summary>
    /// Проверяет скобки в арифметике double, включая приоритеты
    /// </summary>
    [Fact]
    public async Task RespectsParenthesizedDoubleArithmeticPriority()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<double>("parenthesized_double_expression", 124.5d);

        Math.Abs(result - 42d).ShouldBeLessThan(0.0000001d);
    }
}
