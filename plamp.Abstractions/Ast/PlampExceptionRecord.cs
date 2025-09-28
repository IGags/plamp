namespace plamp.Abstractions.Ast;

/// <summary>
/// Обобщённая запись об ошибке. Форматируется в конкретную ошибку.
/// </summary>
public record PlampExceptionRecord
{
    /// <summary>
    /// Шаблон сообщения с полями для записи конкретных значений
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Код ошибки
    /// </summary>
    public required string Code { get; init; }
    
    /// <summary>
    /// Уровень серьёзности ошибки
    /// </summary>
    public required ExceptionLevel Level { get; init; }
}