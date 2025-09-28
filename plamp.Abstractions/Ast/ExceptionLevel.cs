namespace plamp.Abstractions.Ast;

/// <summary>
/// Уровень ошибки(не всегда ошибки)
/// </summary>
public enum ExceptionLevel
{
    /// <summary>
    /// Информация (напр советы по рефакторингу)
    /// </summary>
    Info,
    
    /// <summary>
    /// Предупреждение (возможные ошибки рантайма или неочевидное поведение)
    /// </summary>
    Warning,
    
    /// <summary>
    /// Ошибки компиляции
    /// </summary>
    Error
}