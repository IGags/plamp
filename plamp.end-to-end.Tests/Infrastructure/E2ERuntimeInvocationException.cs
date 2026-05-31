namespace plamp.EndToEnd.Tests.Infrastructure;

/// <summary>
/// Исключение-обёртка для e2e-инфраструктуры.
/// Добавляет к runtime-ошибке контекст .plp файла, вызываемой функции и IL дампа
/// </summary>
/// <param name="filePath">Полный путь к .plp файлу</param>
/// <param name="methodName">Имя вызываемой функции</param>
/// <param name="emittedIl">IL дамп, который был сгенерирован во время эмиссии</param>
/// <param name="innerException">Исходное исключение, полученное при вызове функции</param>
public sealed class E2ERuntimeInvocationException(
    string filePath,
    string methodName,
    string emittedIl,
    Exception innerException)
    : Exception(CreateMessage(filePath, methodName, emittedIl, innerException), innerException)
{
    /// <summary>
    /// Полный путь к .plp файлу
    /// </summary>
    public string FilePath { get; } = filePath;

    /// <summary>
    /// Имя функции, вызов которой завершился исключением
    /// </summary>
    public string MethodName { get; } = methodName;

    /// <summary>
    /// IL дамп, который сгенерировал plamp
    /// </summary>
    public string EmittedIl { get; } = emittedIl;

    /// <summary>
    /// Собирает сообщение об ошибке
    /// </summary>
    private static string CreateMessage(
        string filePath,
        string methodName,
        string emittedIl,
        Exception innerException)
    {
        var rootException = innerException.GetBaseException();

        return $"""
                Ошибка вызова функции {methodName} из файла {filePath}.

                Исключение:
                {innerException}

                Корневая причина:
                {rootException.GetType().FullName}: {rootException.Message}

                IL дамп:
                {emittedIl}
                """;
    }
}
