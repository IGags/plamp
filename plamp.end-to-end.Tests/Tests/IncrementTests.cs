using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты инкрементов и декрементов
/// </summary>
public class IncrementTests
{
    private const string SourceFile = "Increments.plp";

    /// <summary>
    /// Набор сценариев, где результат prefix/postfix операции используется в правой части присваивания
    /// </summary>
    public static TheoryData<string> IncrementOrDecrementAssignments => new()
    {
        "postfix_increment_by_indexer_in_assignment",
        "prefix_increment_by_indexer_in_assignment",
        "postfix_increment_field_in_assignment",
        "prefix_increment_field_in_assignment",
        "postfix_decrement_by_indexer_in_assignment",
        "prefix_decrement_by_indexer_in_assignment",
        "postfix_decrement_field_in_assignment",
        "prefix_decrement_field_in_assignment"
    };

    /// <summary>
    /// Проверяет prefix/postfix инкрементов и декрементов
    /// </summary>
    [Theory]
    [MemberData(nameof(IncrementOrDecrementAssignments))]
    public async Task UsesIncrementOrDecrementInAssignment(string methodName)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>(methodName);

        result.ShouldBe(42);
    }

    /// <summary>
    /// Проверяет, что цель присваивания после инкремента можно сразу использовать в последующем присваивании
    /// </summary>
    [Fact]
    public async Task ReassignsIncrementedTarget()
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>("incremented_assignment_target_then_reassign");

        result.ShouldBe(42);
    }
}
