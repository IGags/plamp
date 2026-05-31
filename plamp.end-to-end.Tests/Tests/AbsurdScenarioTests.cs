using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты бессмысленных, но валидных сценариев
/// </summary>
public class AbsurdScenarioTests
{
    private const string SourceFile = "AbsurdScenarios.plp";

    /// <summary>
    /// Набор странных сценариев, которые не должны ломать компиляцию и runtime.
    /// </summary>
    public static TheoryData<string, object?[]> AbsurdScenarios => new()
    {
        { "call_empty_functions", [123] }, // вызов пустой функции
        { "use_empty_structs", [] }, // использование пустой структуры
        { "use_empty_generic_structs", [] }, // использование пустых дженерик структур
        { "pass_empty_struct_through_generic_identity", [] }, // прогон пустой структуры через дженерик
        { "call_empty_generic_function_with_empty_struct_argument", [] }, // вызов пустой дженерик функции с пустой моделью
        { "ignored_argument_call_chain", [123] }, //  вызов пустых функций, которые ничего не делают
        { "ignore_non_void_call_result", [] }, // игнорирование результата функции, которая возвращает значение
        { "self_assignment_storm", [] }, // самоприсваивание и множественное присваивание одной переменной самой себе
        { "cancel_everything_back_to_start", [] }, // цепочка изменений, которые возвращают исходное значение
        { "mutate_value_then_undo_through_field_and_array", [] }, // мутация поля с откатом через массив
        { "loop_that_wants_to_run_but_breaks_immediately", [] }, // цикл с немедленным break
        { "tautology_driven_branching", [] }, // ветвление на тавтологическом условии
        { "nested_generic_identity_lasagna", [] }, // вложенный дженерик вокруг обычного сложения
        { "intentionally_overwritten_arguments", [1, 2] } // аргументы затираются и перестают иметь смысл
    };

    /// <summary>
    /// Проверяет валидные, но бессмысленные конструкции, которые должны проходить
    /// </summary>
    [Theory]
    [MemberData(nameof(AbsurdScenarios))]
    public async Task RunsAbsurdScenario(string methodName, object?[] arguments)
    {
        var program = await PlampE2ERunner.CompileFromCodeForTestsAsync(SourceFile);

        var result = program.Invoke<int>(methodName, arguments);

        result.ShouldBe(123);
    }
}
