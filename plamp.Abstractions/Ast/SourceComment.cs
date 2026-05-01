namespace plamp.Abstractions.Ast;

/// <summary>
/// Комментарий в коде
/// </summary>
/// <param name="Text">Исходный текст комментария вместе с ограничителями</param>
/// <param name="Position">Позиция комментария в исходном файле</param>
/// <param name="Kind">Вид комментария</param>
public readonly record struct SourceComment(string Text, FilePosition Position, CommentKind Kind);