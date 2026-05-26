using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.Tokenization.Enums;
using plamp.Alternative.Tokenization.Token;

namespace plamp.Alternative.Tokenization;

/// <summary>
/// Выполняет токенизацию файла
/// </summary>
public static class Tokenizer
{
    /// <summary>
    /// Преобразует содержимое файла в последовательность токенов
    /// </summary>
    /// <param name="encoding">Кодировка исходного файла</param>
    /// <param name="fileName">Имя файла, используемое в позициях и диагностике</param>
    /// <param name="fileStream">Стрим исходного файла</param>
    /// <param name="ct">Токен отмены операции</param>
    public static async Task<TokenizationResult> TokenizeAsync(
        Stream fileStream,
        Encoding encoding,
        string fileName,
        CancellationToken ct = default)
    {
        var tokenList = new List<TokenBase>();
        var exceptionList = new List<PlampException>();
        using var context = new TokenizationContext(tokenList, exceptionList, fileName, fileStream, encoding, ct);
        await context.MoveNextAsync();

        while (!context.IsEof)
        {
            if (await TryParseSingleLineCommentAsync(context)) continue;
            if (await TryParseMultilineCommentAsync(context)) continue;
            if (await TryParseWordAsync(context)) continue;
            if (await TryParseOperatorAsync(context)) continue;
            if (await TryParseNumberAsync(context)) continue;
            if (await TryParseStringLiteralAsync(context)) continue;
            if (await TryParseCustomAsync(context)) continue;
            if (await TryParseLineBreak(context)) continue;
            
            var errPos = new FilePosition(context.ByteOffset, context.CurrentByteLength, context.FileName);
            context.Exceptions.Add(new PlampException(PlampExceptionInfo.UnexpectedToken(context.Current.ToString()), errPos));
            await context.MoveNextAsync();
        }

        context.Tokens.Add(new EndOfFile(new FilePosition(fileStream.Length, 0, fileName)));
        
        ReplaceLineBreaksToImplicitEndOfStatements(context.Tokens);
        var sequence = new TokenSequence(context.Tokens);

        return new TokenizationResult(sequence, context.Exceptions);
    }

    #region Words

    internal static void ReplaceLineBreaksToImplicitEndOfStatements(List<TokenBase> sequence)
    {
        if (sequence.Count < 2) return;
        var prev = sequence[0];
        for (var i = 1; i < sequence.Count; i++)
        {
            if (sequence[i] is WhiteSpace { Kind: WhiteSpaceKind.LineBreak } &&
                prev
                is Word
                or Literal
                or CloseParen
                or CloseSquareBracket
                or CloseCurlyBracket
                or KeywordToken { Keyword: Keywords.Return or Keywords.Break or Keywords.Continue })
            {
                sequence[i] = new ImplicitEndOfStatement(sequence[i].Position, sequence[i].GetStringRepresentation());
            }

            if (sequence[i] is not WhiteSpace)
            {
                prev = sequence[i];
            }
        }
    }
    
    /// <summary>
    /// Разбирает идентификатор или ключевое слово, начиная с текущей позиции
    /// </summary>
    /// <param name="context">Контекст токенизации</param>
    /// <returns>Токен ключевого слова или идентификатора</returns>
    private static async Task<bool> TryParseWordAsync(TokenizationContext context)
    {
        if (context.Current != '_' && !char.IsLetter(context.Current)) return false;
        var startOffset = context.ByteOffset;
        var builder = new StringBuilder();
        
        do
        {
            if (char.IsLetterOrDigit(context.Current) || context.Current == '_') builder.Append(context.Current);
            else break;
        } while (await context.MoveNextAsync());

        var byteLen = context.ByteOffset - startOffset;
        var filePosition = new FilePosition(startOffset, byteLen, context.FileName);
        var word = builder.ToString();

        TokenBase token;
        if (word.ToKeyword() != Keywords.Unknown)
        {
            token = new KeywordToken(word, filePosition, word.ToKeyword());
        }
        else
        {
            token = new Word(word, filePosition);
        }

        context.Tokens.Add(token);
        return true;
    }

    #endregion

    #region Numbers

    /// <summary>
    /// Разбирает числовой литерал
    /// </summary>
    /// <param name="context">Контекст токенизации для накопления ошибок</param>
    /// <returns>Токен числового литерала</returns>
    private static async Task<bool> TryParseNumberAsync(TokenizationContext context)
    {
        if (!char.IsDigit(context.Current)) return false;
            
        var builder = new StringBuilder();
        var startOffset = context.ByteOffset;
        var isFractional = false;
        
        do
        {
            if (char.IsDigit(context.Current))
            {
                builder.Append(context.Current);
            }
            else if (!isFractional && context.Current == '.')
            {
                var peek = await context.PeekCharAtAsync(1);
                if (peek != null && char.IsDigit(peek.Value))
                {
                    builder.Append(context.Current);
                    isFractional = true;
                    continue;
                }
                break;
            }
            else
            {
                break;
            }
        } while (await context.MoveNextAsync());

        var postfixBuilder = new StringBuilder();
        do
        {
            if (char.IsLetter(context.Current))
            {
                postfixBuilder.Append(context.Current);
            }
            else
            {
                break;
            }
        } while (await context.MoveNextAsync());
        
        var postfix = postfixBuilder.ToString();
        var numberPart = builder.ToString();
        var filePosition = new FilePosition(startOffset, context.ByteOffset - startOffset, context.FileName);
        if (!TryParseNumberTypePostfix(numberPart, postfix, out var cort))
        {
            context.Exceptions.Add(new PlampException(PlampExceptionInfo.UnknownNumberFormat(), filePosition));
        }

        var (value, type) = cort;
        var lit = new Literal(numberPart + postfix, filePosition, value, type!);
        context.Tokens.Add(lit);
        return true;
    }

    /// <summary>
    /// Преобразует строковое представление числа и его суффикс в CLR-значение
    /// </summary>
    /// <param name="value">Числовая часть литерала без суффикса</param>
    /// <param name="postfix">Суффикс типа</param>
    /// <param name="result">Результирующее значение и соответствующий тип языка</param>
    /// <returns><see langword="true"/>, если литерал распознан корректно; иначе <see langword="false"/>.</returns>
    private static bool TryParseNumberTypePostfix(string value, string postfix, out (object, ITypeInfo?) result)
    {
        switch (postfix)
        {
            case "i":
                var res = int.TryParse(value, CultureInfo.InvariantCulture, out var i);
                result = (i, Builtins.Int);
                return res;
            case "ui":
                res = uint.TryParse(value, CultureInfo.InvariantCulture, out var j);
                result = (j, Builtins.Uint);
                return res;
            case "l":
                res = long.TryParse(value, CultureInfo.InvariantCulture, out var k);
                result = (k, Builtins.Long);
                return res;
            case "ul":
                res = ulong.TryParse(value, CultureInfo.InvariantCulture, out var l);
                result = (l, Builtins.Ulong);
                return res;
            case "d":
                res = double.TryParse(value, CultureInfo.InvariantCulture, out var m);
                result = (m, Builtins.Double);
                return res;
            case "f":
                res = float.TryParse(value, CultureInfo.InvariantCulture, out var n);
                result = (n, Builtins.Float);
                return res;
            case "b":
                res = byte.TryParse(value, CultureInfo.InvariantCulture, out var o);
                result = (o, Builtins.Byte);
                return res;
            case "":
                if (value.Contains('.'))
                {
                    res = double.TryParse(value, CultureInfo.InvariantCulture, out var s);
                    result = (s, Builtins.Double);
                    return res;
                }

                res = long.TryParse(value, CultureInfo.InvariantCulture, out var t);
                if (res)
                {
                    if (t is <= int.MaxValue and >= int.MinValue)
                    {
                        result = ((int)t, Builtins.Int);
                        return true;
                    }

                    result = (t, Builtins.Long);
                    return true;
                }

                result = (t, null);
                return false;
            default:
                result = (new object(), null);
                return false;
        }
    }

    #endregion

    #region Strings

    /// <summary>
    /// Разбирает строковый литерал и обрабатывает escape-последовательности внутри него
    /// </summary>
    /// <param name="context">Контекст токенизации для накопления ошибок</param>
    /// <returns>Токен строкового литерала</returns>
    private static async Task<bool> TryParseStringLiteralAsync(TokenizationContext context)
    {
        if(context.Current != '"') return false;
        
        var builder = new StringBuilder();
        var startOffset = context.ByteOffset;
        var breakCycle = false;
        while (await context.MoveNextAsync())
        {
            if(await context.IsExprEnd) break;
            
            switch (context.Current)
            {
                case '"':
                    await context.MoveNextAsync();
                    var lit = new Literal(
                        $"\"{builder}\"",
                        new FilePosition(startOffset, context.ByteOffset - startOffset, context.FileName),
                        builder.ToString(),
                        Builtins.String);
                    context.Tokens.Add(lit);
                    return true;
                case '\\':
                    var escapeStartOffset = context.ByteOffset;
                    if(!await context.MoveNextAsync())
                    {
                        breakCycle = true;
                        break;
                    }
                    TryParseEscapedSequence(escapeStartOffset, builder, context);
                    break;
                default:
                    builder.Append(context.Current);
                    break;
            }
            
            if(breakCycle) break;
        }

        var endPosition = new FilePosition(startOffset, context.ByteOffset - startOffset, context.FileName);
        context.Exceptions.Add(new PlampException(PlampExceptionInfo.StringIsNotClosed(), endPosition));
        var literal = new Literal($"\"{builder}", endPosition, builder.ToString(), Builtins.String);
        context.Tokens.Add(literal);
        return true;
    }

    /// <summary>
    /// Пытается разобрать escape-последовательность внутри строкового литерала
    /// </summary>
    /// <param name="escapeStartOffset">Байтовое смещение с которого начинается escape-последовательность</param>
    /// <param name="builder">Накопитель результирующего строкового значения</param>
    /// <param name="context">Контекст токенизации для накопления ошибок</param>
    private static void TryParseEscapedSequence(
        long escapeStartOffset,
        StringBuilder builder,
        TokenizationContext context)
    {
        switch (context.Current)
        {
            case 'n':
                builder.Append('\n');
                break;
            case 'r':
                builder.Append('\r');
                break;
            case '\\':
                builder.Append('\\');
                break;
            case 't':
                builder.Append('\t');
                break;
            case '"':
                builder.Append('"');
                break;
            default:
                context.Exceptions.Add(
                    new PlampException(
                        PlampExceptionInfo.InvalidEscapeSequence($"\\{context.Current}"),
                        new FilePosition(escapeStartOffset, context.Encoding.GetByteCount("\\") + context.CurrentByteLength, context.FileName)));
                return;
        }
    }

    #endregion

    #region Custom

    /// <summary>
    /// Разбирает одиночные служебные символы, операторы, пробельные токены и комментарии
    /// </summary>
    /// <param name="context">Контекст токенизации для накопления ошибок</param>
    /// <returns><see langword="true"/>, если токен был распознан; иначе <see langword="false"/></returns>
    private static async Task<bool> TryParseCustomAsync(TokenizationContext context)
    {
        var startOffset = context.ByteOffset;
        var filePosition = new FilePosition(startOffset, context.CurrentByteLength, context.FileName);

        var next = await context.PeekCharAtAsync(1);
        
        switch (context.Current)
        {
            case '{':
                context.Tokens.Add(new OpenCurlyBracket(filePosition));
                await context.MoveNextAsync();
                return true;
            case '}':
                context.Tokens.Add(new CloseCurlyBracket(filePosition));
                await context.MoveNextAsync();
                return true;
            case '[':
                context.Tokens.Add(new OpenSquareBracket(filePosition));
                await context.MoveNextAsync();
                return true;
            case ']':
                context.Tokens.Add(new CloseSquareBracket(filePosition));
                await context.MoveNextAsync();
                return true;
            case '(':
                context.Tokens.Add(new OpenParen(filePosition));
                await context.MoveNextAsync();
                return true;
            case ')':
                context.Tokens.Add(new CloseParen(filePosition));
                await context.MoveNextAsync();
                return true;
            case ',':
                context.Tokens.Add(new Comma(filePosition));
                await context.MoveNextAsync();
                return true;
            case ';':
                context.Tokens.Add(new EndOfStatement(filePosition));
                await context.MoveNextAsync();
                return true;
            case ' ':
                context.Tokens.Add(new WhiteSpace(" ", filePosition, WhiteSpaceKind.WhiteSpace));
                await context.MoveNextAsync();
                return true;
            case '\t':
                context.Tokens.Add(new WhiteSpace("\t", filePosition, WhiteSpaceKind.WhiteSpace));
                await context.MoveNextAsync();
                return true;
            case ':' when next != '=':
                context.Tokens.Add(new Colon(filePosition));
                await context.MoveNextAsync();
                return true;
        }
        return false;
    }

    /// <summary>
    /// Парсинг комментария длиной в одну строку.
    /// </summary>
    /// <param name="context">Контекст токенизации</param>
    /// <returns>Успешность операции парсинга</returns>
    private static async Task<bool> TryParseSingleLineCommentAsync(
        TokenizationContext context)
    {
        if (context.Current != '/') return false;
        var next = await context.PeekCharAtAsync(1);
        if (next is not '/') return false;
        await context.MoveNextAsync();
        var start = context.ByteOffset;

        var sb = new StringBuilder("//");
        while (await context.MoveNextAsync() && !await context.IsExprEnd)
        {
            sb.Append(context.Current);
        }
        
        var filePosition = new FilePosition(start, context.ByteOffset - start, context.FileName);
        var token = new WhiteSpace(sb.ToString(), filePosition, WhiteSpaceKind.SingleLineComment);
        context.Tokens.Add(token);
        return true;
    }

    /// <summary>
    /// Инкапсулирует всю логику парсинга комментариев, в случае true возвращает позицию сразу после комментария, из-за этого не требует перерасчёта byteOffset после себя.
    /// </summary>
    /// <param name="context">Контекст, в который записываются возможные ошибки</param>
    /// <returns></returns>
    private static async Task<bool> TryParseMultilineCommentAsync(TokenizationContext context)
    {
        if (context.Current != '/') return false;
        var next = await context.PeekCharAtAsync(1);
        if(next is not '*') return false;
        var start = context.ByteOffset;
        await context.MoveNextAsync();

        var closed = false;
        var commentBuilder = new StringBuilder("/*");
        while (await context.MoveNextAsync())
        {
            if (context.Current == '*')
            {
                next = await context.PeekCharAtAsync(1);
                if (next is not '/')
                {
                    commentBuilder.Append('*');
                    continue;
                }
                
                await context.MoveNextAsync();
                await context.MoveNextAsync();
                closed = true;
                commentBuilder.Append("*/");
                break;
            }
            
            commentBuilder.Append(context.Current);
        }

        var token = new WhiteSpace(commentBuilder.ToString(), new FilePosition(start, context.ByteOffset - start, context.FileName), WhiteSpaceKind.MultiLineComment);
        if (!closed)
        {
            var filePos = new FilePosition(start, context.ByteOffset - start, context.FileName);
            context.Exceptions.Add(new PlampException(PlampExceptionInfo.CommentIsNotClosed(), filePos));
        }
        context.Tokens.Add(token);
        return true;
    }

    /// <summary>
    /// Пытается разобрать перенос строки
    /// </summary>
    /// <param name="context">Контекст токенизации</param>
    /// <returns>Флаг успеха операции</returns>
    private static async Task<bool> TryParseLineBreak(TokenizationContext context)
    {
        if (!await context.IsEol()) return false;

        if (context.Current == '\n')
        {
            var filePos = new FilePosition(context.ByteOffset, context.CurrentByteLength, context.FileName);
            var token = new WhiteSpace("\n", filePos, WhiteSpaceKind.LineBreak);
            await context.MoveNextAsync();
            context.Tokens.Add(token);
            return true;
        }

        var startOffset = context.ByteOffset;
        var byteLength = context.CurrentByteLength;
        await context.MoveNextAsync();
        byteLength += context.CurrentByteLength;
        
        var pos = new FilePosition(startOffset, byteLength, context.FileName);
        var tok = new WhiteSpace("\r\n", pos, WhiteSpaceKind.LineBreak);
        await context.MoveNextAsync();
        context.Tokens.Add(tok);
        return true;
    }

    /// <summary>
    /// Пытается разобрать оператор
    /// </summary>
    /// <param name="context">Контекст токенизации</param>
    /// <returns><see langword="true"/>, если оператор успешно распознан; иначе <see langword="false"/>.</returns>
    private static async Task<bool> TryParseOperatorAsync(
        TokenizationContext context)
    {
        var startOffset = context.ByteOffset;
        var current = context.Current;
        var next = await context.PeekCharAtAsync(1);
        
        if (next != null)
        {
            var op = $"{current}{next}";
            var opPos = new FilePosition(startOffset, context.Encoding.GetByteCount(op), context.FileName);
            
            switch (op)
            {
                case "++":
                case "--":
                case ":=":
                case "!=":
                case "<=":
                case ">=":
                case "&&":
                case "||":
                    await context.MoveNextAsync();
                    await context.MoveNextAsync();
                    var @operator = new OperatorToken(op, opPos, op.ToOperator());
                    context.Tokens.Add(@operator);
                    return true;
            }
        }

        switch (current)
        {
            case '+':
            case '-':
            case '=':
            case '/':
            case '*':
            case '!':
            case '%':
            case '|':
            case '&':
            case '^':
            case '<':
            case '>':
            case '.':
                var opString = current.ToString();
                var filePosition = new FilePosition(startOffset, context.CurrentByteLength, context.FileName);
                var @operator = new OperatorToken(opString, filePosition, opString.ToOperator());
                context.Tokens.Add(@operator);
                await context.MoveNextAsync();
                return true;
            default:
                return false;
        }
    }

    #endregion
}
