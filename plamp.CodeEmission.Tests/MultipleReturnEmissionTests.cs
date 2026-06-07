using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using plamp.Alternative;
using plamp.ILCodeEmitters;
using Shouldly;

namespace plamp.CodeEmission.Tests;

/// <summary>
/// Интеграционные тесты физического CLR-представления множественного возврата.
/// </summary>
public class MultipleReturnEmissionTests
{
    /// <summary>
    /// Проверяет порядок пользовательских аргументов, out-параметров результата и CLR-результата.
    /// </summary>
    [Fact]
    public async Task EmitsUserArgumentsBeforeOutReturnParameters()
    {
        var module = await CompileAsync(
            """
            module integration.multiple_return

            fn inspect(value: int) int, string, bool {
                return value, "ready", true
            }
            """);
        var method = module.GetMethod("inspect")!;
        var parameters = method.GetParameters();
        object?[] arguments = [31, null, null];

        method.ReturnType.ShouldBe(typeof(bool));
        parameters.Select(x => x.ParameterType).ShouldBe(
        [
            typeof(int),
            typeof(int).MakeByRefType(),
            typeof(string).MakeByRefType()
        ]);
        parameters.Select(x => x.IsOut).ShouldBe([false, true, true]);
        method.Invoke(null, arguments).ShouldBe(true);
        arguments.ShouldBe([31, 31, "ready"]);
    }

    /// <summary>
    /// Проверяет передачу всех результатов между двумя сгенерированными функциями.
    /// </summary>
    [Fact]
    public async Task GeneratedCallerReceivesAllResults()
    {
        var module = await CompileAsync(
            """
            module integration.multiple_return

            fn values() int, int, int {
                return 7, 11, 13
            }

            fn sum() int {
                first, second, third := values()
                return first + second + third
            }
            """);

        module.GetMethod("sum")!.Invoke(null, []).ShouldBe(31);
    }

    /// <summary>
    /// Проверяет закрытие generic-сигнатуры с out-параметром результата.
    /// </summary>
    [Fact]
    public async Task EmitsGenericOutReturnParameter()
    {
        var module = await CompileAsync(
            """
            module integration.multiple_return

            fn duplicate[T](value: T) T, T {
                return value, value
            }
            """);
        var method = module.GetMethod("duplicate")!.MakeGenericMethod(typeof(string));
        object?[] arguments = ["text", null];

        method.ReturnType.ShouldBe(typeof(string));
        method.GetParameters()[1].ParameterType.ShouldBe(typeof(string).MakeByRefType());
        method.Invoke(null, arguments).ShouldBe("text");
        arguments[1].ShouldBe("text");
    }

    /// <summary>
    /// Проверяет отсутствие ограничения на количество физических out-параметров.
    /// </summary>
    [Fact]
    public async Task EmitsMoreResultsThanValueTupleArity()
    {
        var module = await CompileAsync(
            """
            module integration.multiple_return

            fn many() int, int, int, int, int, int, int, int, int, int {
                return 2, 3, 5, 7, 11, 13, 17, 19, 23, 29
            }
            """);
        var method = module.GetMethod("many")!;
        object?[] arguments = new object?[9];

        method.GetParameters().Length.ShouldBe(9);
        method.GetParameters().ShouldAllBe(x => x.IsOut);
        method.Invoke(null, arguments).ShouldBe(29);
        arguments.ShouldBe([2, 3, 5, 7, 11, 13, 17, 19, 23]);
    }

    /// <summary>
    /// Проверяет, что одиночный результат сохраняет обычную CLR-сигнатуру.
    /// </summary>
    [Fact]
    public async Task KeepsSingleResultSignatureWithoutOutReturnParameters()
    {
        var module = await CompileAsync(
            """
            module integration.multiple_return

            fn single(value: int) string {
                return "single"
            }
            """);
        var method = module.GetMethod("single")!;

        method.ReturnType.ShouldBe(typeof(string));
        method.GetParameters().Select(x => x.ParameterType).ShouldBe([typeof(int)]);
        method.GetParameters().ShouldAllBe(x => !x.IsOut);
        method.Invoke(null, [67]).ShouldBe("single");
    }

    /// <summary>
    /// Проверяет, что out-результаты публикуются только после вычисления всех выражений return.
    /// </summary>
    [Fact]
    public async Task DoesNotPublishOutResultsWhenLaterReturnExpressionThrows()
    {
        var module = await CompileAsync(
            """
            module integration.multiple_return

            fn failing(divisor: int) int, int {
                return 71, 1 / divisor
            }
            """);
        var method = module.GetMethod("failing")!;
        object?[] arguments = [0, 79];

        Should.Throw<TargetInvocationException>(() => method.Invoke(null, arguments));
        arguments[1].ShouldBe(79);
    }

    /// <summary>
    /// Проверяет неявное приведение int-выражений ко всем объявленным числовым типам множественного возврата.
    /// </summary>
    [Fact]
    public async Task ImplicitlyCastsEveryMultipleReturnValue()
    {
        var module = await CompileAsync(
            """
            module integration.multiple_return

            fn numeric_values() long, float, double {
                return 73, 79, 83
            }
            """);
        var method = module.GetMethod("numeric_values")!;
        object?[] arguments = [null, null];

        method.ReturnType.ShouldBe(typeof(double));
        method.GetParameters().Select(x => x.ParameterType).ShouldBe(
        [
            typeof(long).MakeByRefType(),
            typeof(float).MakeByRefType()
        ]);
        method.Invoke(null, arguments).ShouldBe(83d);
        arguments.ShouldBe([73L, 79f]);
    }

    /// <summary>
    /// Проверяет неявное приведение результатов при распаковке в ранее объявленные переменные.
    /// </summary>
    [Fact]
    public async Task ImplicitlyCastsMultipleResultsToExistingVariables()
    {
        var module = await CompileAsync(
            """
            module integration.multiple_return

            fn integers() int, int, int {
                return 103, 107, 109
            }

            fn assign_results() double {
                longValue := 0l
                floatValue := 0f
                doubleValue := 0d
                longValue, floatValue, doubleValue := integers()
                return longValue + floatValue + doubleValue
            }
            """);

        module.GetMethod("assign_results")!.Invoke(null, []).ShouldBe(319d);
    }

    /// <summary>
    /// Проверяет прямой проброс результатов другой функции через return
    /// </summary>
    [Fact]
    public async Task ForwardsMultipleResultsThroughReturn()
    {
        var module = await CompileAsync(
            """
            module integration.multiple_return
            fn source() int, string { return 127, "forwarded"; }
            fn forward() int, string { return source(); }
            """);
        var method = module.GetMethod("forward")!;
        object?[] arguments = [null];

        method.Invoke(null, arguments).ShouldBe("forwarded");
        arguments[0].ShouldBe(127);
    }

    /// <summary>
    /// Проверяет проброс результатов дженерик функции
    /// </summary>
    [Fact]
    public async Task ForwardsGenericMultipleResultsThroughReturn()
    {
        var module = await CompileAsync(
            """
            module integration.multiple_return
            fn duplicate[T](value: T) T, T { return value, value; }
            fn forward(value: string) string, string { return duplicate(value); }
            """);
        var method = module.GetMethod("forward")!;
        object?[] arguments = ["generic", null];

        method.Invoke(null, arguments).ShouldBe("generic");
        arguments[1].ShouldBe("generic");
    }

    /// <summary>
    /// Проверяет проброс большого количества результатов
    /// </summary>
    [Fact]
    public async Task ForwardsManyResultsThroughReturn()
    {
        var module = await CompileAsync(
            """
            module integration.multiple_return
            fn source() int, int, int, int, int, int, int, int, int {
                return 2, 3, 5, 7, 11, 13, 17, 19, 23;
            }
            fn forward() int, int, int, int, int, int, int, int, int { return source(); }
            """);
        var method = module.GetMethod("forward")!;
        object?[] arguments = new object?[8];

        method.Invoke(null, arguments).ShouldBe(23);
        arguments.ShouldBe([2, 3, 5, 7, 11, 13, 17, 19]);
    }

    /// <summary>
    /// Проверяет возврат вызовов и обычных выражений слева направо
    /// </summary>
    [Fact]
    public async Task EvaluatesCallsInsideReturnFromLeftToRight()
    {
        var module = await CompileAsync(
            """
            module integration.multiple_return
            fn mutate(values: []int, value: int) int {
                values[0] := value;
                return values[0];
            }
            fn values(items: []int) int, int, int {
                return mutate(items, 29), mutate(items, 31), items[0];
            }
            fn verify() int {
                items := [1]int;
                first, second, observed := values(items);
                return first * 10000 + second * 100 + observed;
            }
            """);

        module.GetMethod("verify")!.Invoke(null, []).ShouldBe(293131);
    }

    /// <summary>
    /// Проверяет разворачивание множественного вызова среди других выражений return
    /// </summary>
    [Fact]
    public async Task ReturnsMultipleResultCallAlongsideExpression()
    {
        var module = await CompileAsync(
            """
            module integration.multiple_return
            fn pair() int, string { return 157, "mixed"; }
            fn values() int, int, string { return 151, pair(); }
            """);
        var method = module.GetMethod("values")!;
        object?[] arguments = [null, null];

        method.Invoke(null, arguments).ShouldBe("mixed");
        arguments.ShouldBe([151, 157]);
    }

    private static async Task<ModuleBuilder> CompileAsync(string code)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(code));
        var (exceptions, symTable) = await CompilationPipeline.RunFrontendSteps(stream, Encoding.UTF8, "integration.plp");
        exceptions.ShouldBeEmpty();

        var assemblyName = new AssemblyName(Guid.NewGuid().ToString("N"));
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule(assemblyName.Name!);
        SymTableEmitter.EmitModule(symTable, module);
        module.CreateGlobalFunctions();
        return module;
    }
}
