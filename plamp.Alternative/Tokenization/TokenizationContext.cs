using System.Collections.Generic;
using System.Text;
using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization.Token;

namespace plamp.Alternative.Tokenization;

/// <summary>
/// Хранит состояние токенизации
/// </summary>
internal class TokenizationContext
{
    public TokenizationContext(List<TokenBase> tokens, List<PlampException> exceptions)
    {
        Tokens = tokens;
        Exceptions = exceptions;
    }

    /// <summary>
    /// Накопленные токены файла
    /// </summary>
    public List<TokenBase> Tokens { get; }

    /// <summary>
    /// Накопленные ошибки токенизации
    /// </summary>
    public List<PlampException> Exceptions { get; }

    /// <summary>
    /// Признак того, что токенизатор находится внутри многострочного комментария
    /// </summary>
    public bool IsInsideMultiLineComment { get; set; }

    /// <summary>
    /// Накопитель текста текущего многострочного комментария
    /// </summary>
    public StringBuilder MultiLineCommentBuilder { get; } = new();

    /// <summary>
    /// Смещение начала текущего многострочного комментария в байтах
    /// </summary>
    public int MultiLineCommentStartOffset { get; set; }

    /// <summary>
    /// Имя файла, в котором начался текущий многострочный комментарий
    /// </summary>
    public string MultiLineCommentFileName { get; set; } = string.Empty;

    /// <summary>
    /// Признак того, что перед продолжением многострочного комментария
    /// нужно добавить фактический перевод строки из предыдущей прочитанной строки
    /// </summary>
    public bool ShouldAppendLineBreakBeforeContinuingComment { get; set; }
}