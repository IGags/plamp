using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты runtime ошибок: проверяет случаи, когда валидный код на plamp может падать с CLR исключением
/// </summary>
public class RuntimeErrorTests
{
    private const string SourceFile = "RuntimeErrors.plp";

    /// <summary>
    /// Набор сценариев, которые компилируются plamp-ом, но падают при выполнении
    /// </summary>
    public static TheoryData<string, Type, string> RuntimeFailures => new()
    {
        { "divide_by_zero", typeof(DivideByZeroException), "div" },
        { "read_array_out_of_range", typeof(IndexOutOfRangeException), "ldelem" },
        { "write_array_out_of_range", typeof(IndexOutOfRangeException), "stelem" },
        { "read_array_negative_index", typeof(IndexOutOfRangeException), "ldelem" },
        { "read_array_negative_literal_index", typeof(IndexOutOfRangeException), "ldelem" },
        { "write_array_negative_index", typeof(IndexOutOfRangeException), "stelem" }
    };

    /// <summary>
    /// Проверяет, что ошибки доходят до выполнения и оборачиваются с сохранением корневого CLR исключения
    /// </summary>
    [Theory]
    [MemberData(nameof(RuntimeFailures))]
    public async Task TestRuntimeErrorWithIlDump(
        string methodName,
        Type expectedRootExceptionType,
        string expectedIlInstruction)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var exception = Should.Throw<E2ERuntimeInvocationException>(
            () => program.Invoke<int>(methodName));

        exception.InnerException.ShouldNotBeNull().GetBaseException().GetType().ShouldBe(expectedRootExceptionType);
        exception.EmittedIl.ShouldContain(expectedIlInstruction);
    }
}
