using plamp.Abstractions.Ast;

namespace plamp.EndToEnd.Tests.Infrastructure;

/// <summary>
/// Исключение-обёртка, для e2e-инфраструктуры
/// </summary>
/// <param name="filePath">Полный путь к .plp файлу, который не удалось скомпилировать</param>
/// <param name="diagnostics">Список ошибок при сборке</param>
internal sealed class E2ECompilationException(string filePath, IReadOnlyList<PlampException> diagnostics)
    : Exception(CreateMessage(filePath, diagnostics))
{
    /// <summary>
    /// Полный путь к .plp файлу, на котором был остановлен e2e-сценарий
    /// </summary>
    public string FilePath { get; } = filePath;

    /// <summary>
    /// Ошибки полученные при сборке
    /// </summary>
    public IReadOnlyList<PlampException> Diagnostics { get; } = diagnostics;

    private static string CreateMessage(string filePath, IReadOnlyList<PlampException> diagnostics)
    {
        var diagnosticMessages = diagnostics.Select(x => x.ToString());
        return $"Файл {filePath} не прошёл компиляцию:{Environment.NewLine}{string.Join(Environment.NewLine, diagnosticMessages)}";
    }
}
