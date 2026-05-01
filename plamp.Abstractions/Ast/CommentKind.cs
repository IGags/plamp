namespace plamp.Abstractions.Ast;

/// <summary>
/// Определяет вид комментария в коде
/// </summary>
public enum CommentKind
{
    /// <summary>
    /// Однострочный комментарий, начинающийся с <c>//</c>
    /// </summary>
    SingleLine,

    /// <summary>
    /// Многострочный комментарий, ограниченный <c>/*</c> и <c>*/</c>
    /// </summary>
    MultiLine
}