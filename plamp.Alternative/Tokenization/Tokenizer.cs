using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    /// <param name="fileReader">Поток чтения исходного файла</param>
    /// <param name="fileName">Имя файла, используемое в позициях и диагностике</param>
    /// <param name="token">Токен отмены</param>
    public static async Task<TokenizationResult> TokenizeAsync(
        StreamReader fileReader,
        string fileName,
        CancellationToken token = default)
    {
        var tokenList = new List<TokenBase>();
        var exceptionList = new List<PlampException>();
        var context = new TokenizationContext(tokenList, exceptionList);
        var byteOffset = 0;
        var encoding = fileReader.CurrentEncoding;

        while (await fileReader.ReadLineAsync(token) is { } line)
        {
            if (context.ShouldAppendLineBreakBeforeContinuingComment)
            {
                context.MultiLineCommentBuilder.Append('\n');
                byteOffset += encoding.GetByteCount("\n");
                context.ShouldAppendLineBreakBeforeContinuingComment = false;
            }

            ProcessLine(line, fileName, encoding, context, ref byteOffset);

            if (context.IsInsideMultiLineComment)
            {
                context.ShouldAppendLineBreakBeforeContinuingComment = true;
                continue;
            }

            context.Tokens.Add(new WhiteSpace("\n", new FilePosition(byteOffset, 1, fileName), WhiteSpaceKind.LineBreak));
            byteOffset += encoding.GetByteCount("\n");
        }

        if (context.IsInsideMultiLineComment)
        {
            CompleteUnclosedMultiLineComment(context);
        }

        context.Tokens.Add(new EndOfFile(new FilePosition(byteOffset, 0, fileName)));
        return new TokenizationResult(new TokenSequence(context.Tokens), context.Exceptions);
    }

    /// <summary>
    /// Обрабатывает одну строку исходного файла
    /// </summary>
    /// <param name="line">Строка без символа перевода строки</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="encoding">Кодировка файла</param>
    /// <param name="context">Контекст токенизации</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    private static void ProcessLine(
        string line,
        string fileName,
        Encoding encoding,
        TokenizationContext context,
        ref int byteOffset)
    {
        for (var i = 0; i < line.Length;)
        {
            var prevIx = i;

            if (context.IsInsideMultiLineComment)
            {
                ContinueParsingMultiLineComment(line, ref i, context, fileName, byteOffset);
            }
            else if (char.IsLetter(line[i]))
            {
                context.Tokens.Add(ParseWord(line, ref i, byteOffset, fileName));
            }
            else if (char.IsDigit(line[i]))
            {
                context.Tokens.Add(ParseNumber(line, ref i, byteOffset, fileName, context));
            }
            else if (line[i] == '"')
            {
                context.Tokens.Add(ParseStringLiteral(line, ref i, byteOffset, fileName, encoding, context));
            }
            else
            {
                if (TryParseCustom(line, ref i, byteOffset, fileName, out var result, context))
                {
                    if (result != null)
                    {
                        context.Tokens.Add(result);
                    }
                }
            }

            byteOffset += encoding.GetByteCount(line.AsSpan().Slice(prevIx, i - prevIx));
        }
    }

    #region Words

    /// <summary>
    /// Разбирает идентификатор или ключевое слово, начиная с текущей позиции
    /// </summary>
    /// <param name="text">Текущая строка исходного файла</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на первый символ после слова</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    /// <param name="fileName">Имя файла</param>
    /// <returns>Токен ключевого слова или идентификатора</returns>
    private static TokenBase ParseWord(string text, ref int position, int byteOffset, string fileName)
    {
        var builder = new StringBuilder();
        do
        {
            if (char.IsLetterOrDigit(text[position]) || text[position] == '_')
            {
                builder.Append(text[position]);
                position++;
            }
            else
            {
                break;
            }
        } while (position < text.Length);

        var filePosition = new FilePosition(byteOffset, builder.Length, fileName);
        var word = builder.ToString();

        if (word.ToKeyword() != Keywords.Unknown)
        {
            return new KeywordToken(word, filePosition, word.ToKeyword());
        }

        return new Word(word, filePosition);
    }

    #endregion

    #region Numbers

    /// <summary>
    /// Разбирает числовой литерал
    /// </summary>
    /// <param name="text">Текущая строка исходного файла</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на первый символ после литерала</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="context">Контекст токенизации для накопления ошибок</param>
    /// <returns>Токен числового литерала</returns>
    private static Literal ParseNumber(string text, ref int position, int byteOffset, string fileName, TokenizationContext context)
    {
        var builder = new StringBuilder();
        var isFractional = false;
        do
        {
            if (char.IsDigit(text[position]))
            {
                builder.Append(text[position]);
                position++;
            }
            else if (!isFractional
                     && text[position] == '.'
                     && position + 1 < text.Length
                     && char.IsDigit(text[position + 1]))
            {
                isFractional = true;
                builder.Append(text[position]);
                position++;
            }
            else
            {
                break;
            }
        } while (position < text.Length);

        var postfixBuilder = new StringBuilder();
        while (position < text.Length)
        {
            if (char.IsLetter(text[position]))
            {
                postfixBuilder.Append(text[position]);
                position++;
            }
            else
            {
                break;
            }
        }

        var postfix = postfixBuilder.ToString();
        var numberPart = builder.ToString();
        var filePosition = new FilePosition(byteOffset, builder.Length + postfix.Length, fileName);
        if (!TryParseNumberTypePostfix(numberPart, postfix, out var cort))
        {
            context.Exceptions.Add(new PlampException(PlampExceptionInfo.UnknownNumberFormat(), filePosition));
        }

        var (value, type) = cort;
        return new Literal(numberPart + postfix, filePosition, value, type!);
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
    /// <param name="text">Текущая строка исходного файла</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на первый символ после литерала</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="fileEncoding">Кодировка файла</param>
    /// <param name="context">Контекст токенизации для накопления ошибок</param>
    /// <returns>Токен строкового литерала</returns>
    private static Literal ParseStringLiteral(
        string text,
        ref int position,
        int byteOffset,
        string fileName,
        Encoding fileEncoding,
        TokenizationContext context)
    {
        var builder = new StringBuilder();
        var start = position;
        position++;
        for (; position < text.Length; position++)
        {
            switch (text[position])
            {
                case '"':
                    position++;
                    return new Literal(
                        $"\"{builder}\"",
                        new FilePosition(byteOffset, position - start, fileName),
                        builder.ToString(),
                        Builtins.String);
                case '\\':
                    position++;
                    if (position >= text.Length)
                    {
                        position = text.Length;
                        break;
                    }

                    TryParseEscapedSequence(text, ref position, byteOffset, fileName, fileEncoding, builder, context);
                    break;
                default:
                    builder.Append(text[position]);
                    break;
            }
        }

        var endPosition = new FilePosition(byteOffset, position - start, fileName);
        context.Exceptions.Add(new PlampException(PlampExceptionInfo.StringIsNotClosed(), endPosition));
        return new Literal($"\"{builder}", endPosition, builder.ToString(), Builtins.String);
    }

    /// <summary>
    /// Пытается разобрать escape-последовательность внутри строкового литерала
    /// </summary>
    /// <param name="text">Текущая строка исходного файла</param>
    /// <param name="position">Позиция символа сразу после обратного слеша</param>
    /// <param name="byteOffset">Смещение начала строкового литерала в байтах</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="fileEncoding">Кодировка файла, используемая для вычисления смещений в диагностике</param>
    /// <param name="builder">Накопитель результирующего строкового значения</param>
    /// <param name="context">Контекст токенизации для накопления ошибок</param>
    private static void TryParseEscapedSequence(
        string text,
        ref int position,
        int byteOffset,
        string fileName,
        Encoding fileEncoding,
        StringBuilder builder,
        TokenizationContext context)
    {
        switch (text[position])
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
                        PlampExceptionInfo.InvalidEscapeSequence($"\\{text[position]}"),
                        new FilePosition(byteOffset + fileEncoding.GetByteCount(builder.ToString()) + fileEncoding.GetByteCount("\""), 2, fileName)));
                return;
        }
    }

    #endregion

    #region Custom

    /// <summary>
    /// Разбирает одиночные служебные символы, операторы, пробельные токены и комментарии
    /// </summary>
    /// <param name="text">Текущая строка исходного файла</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на первый символ после разобранного токена</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="result">Разобранный токен. Для незакрытого многострочного комментария возвращает <see langword="null"/> до его завершения</param>
    /// <param name="context">Контекст токенизации для накопления ошибок</param>
    /// <returns><see langword="true"/>, если токен был распознан; иначе <see langword="false"/></returns>
    private static bool TryParseCustom(
        string text,
        ref int position,
        int byteOffset,
        string fileName,
        out TokenBase? result,
        TokenizationContext context)
    {
        result = null;
        var filePosition = new FilePosition(byteOffset, 1, fileName);
        char? next = position + 1 < text.Length ? text[position + 1] : null;
        switch (text[position])
        {
            case '{':
                result = new OpenCurlyBracket(filePosition);
                position++;
                return true;
            case '}':
                result = new CloseCurlyBracket(filePosition);
                position++;
                return true;
            case '[':
                result = new OpenSquareBracket(filePosition);
                position++;
                return true;
            case ']':
                result = new CloseSquareBracket(filePosition);
                position++;
                return true;
            case '(':
                result = new OpenParen(filePosition);
                position++;
                return true;
            case ')':
                result = new CloseParen(filePosition);
                position++;
                return true;
            case ',':
                result = new Comma(filePosition);
                position++;
                return true;
            case ';':
                result = new EndOfStatement(filePosition);
                position++;
                return true;
            case ' ':
                result = new WhiteSpace(" ", filePosition, WhiteSpaceKind.WhiteSpace);
                position++;
                return true;
            case '\t':
                result = new WhiteSpace("\t", filePosition, WhiteSpaceKind.WhiteSpace);
                position++;
                return true;
            case ':':
                if (next != '=')
                {
                    result = new Colon(filePosition);
                    position++;
                    return true;
                }

                break;
            case '/':
                if (next == '/')
                {
                    result = ParseSingleLineComment(text, ref position, byteOffset, fileName);
                    return true;
                }

                if (next == '*')
                {
                    BeginMultiLineComment(context, byteOffset, fileName);
                    position += 2;
                    ContinueParsingMultiLineComment(text, ref position, context, fileName, byteOffset);
                    return true;
                }

                break;
        }

        if (TryParseOperator(text, ref position, byteOffset, fileName, out var @operator))
        {
            result = @operator;
            return true;
        }

        context.Exceptions.Add(new PlampException(PlampExceptionInfo.UnexpectedToken(text[position].ToString()), filePosition));
        position++;
        return false;
    }

    /// <summary>
    /// Разбирает однострочный комментарий до конца текущей строки.
    /// </summary>
    /// <param name="text">Текущая строка исходного файла.</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на конец строки.</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла.</param>
    /// <param name="fileName">Имя файла.</param>
    /// <returns>Токен trivia для однострочного комментария.</returns>
    private static WhiteSpace ParseSingleLineComment(string text, ref int position, int byteOffset, string fileName)
    {
        var start = position;
        position = text.Length;
        var comment = text[start..position];
        return new WhiteSpace(comment, new FilePosition(byteOffset, comment.Length, fileName), WhiteSpaceKind.SingleLineComment);
    }

    /// <summary>
    /// Инициализирует состояние разбора многострочного комментария
    /// </summary>
    /// <param name="context">Контекст токенизации</param>
    /// <param name="byteOffset">Смещение начала комментария в байтах</param>
    /// <param name="fileName">Имя файла</param>
    private static void BeginMultiLineComment(TokenizationContext context, int byteOffset, string fileName)
    {
        context.IsInsideMultiLineComment = true;
        context.MultiLineCommentBuilder.Clear();
        context.MultiLineCommentBuilder.Append("/*");
        context.MultiLineCommentStartOffset = byteOffset;
        context.MultiLineCommentFileName = fileName;
    }

    /// <summary>
    /// Продолжает разбор многострочного комментария на текущей строке
    /// </summary>
    /// <param name="text">Текущая строка исходного файла</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на первый символ после комментария или на конец строки</param>
    /// <param name="context">Контекст токенизации</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    private static void ContinueParsingMultiLineComment(
        string text,
        ref int position,
        TokenizationContext context,
        string fileName,
        int byteOffset)
    {
        while (position < text.Length)
        {
            if (position + 1 < text.Length
                && text[position] == '*'
                && text[position + 1] == '/')
            {
                context.MultiLineCommentBuilder.Append("*/");
                position += 2;
                var commentText = context.MultiLineCommentBuilder.ToString();
                context.Tokens.Add(
                    new WhiteSpace(
                        commentText,
                        new FilePosition(context.MultiLineCommentStartOffset, commentText.Length, context.MultiLineCommentFileName),
                        WhiteSpaceKind.MultiLineComment));
                context.MultiLineCommentBuilder.Clear();
                context.IsInsideMultiLineComment = false;
                return;
            }

            context.MultiLineCommentBuilder.Append(text[position]);
            position++;
        }
    }

    /// <summary>
    /// Завершает незакрытый многострочный комментарий на конце файла и формирует ошибку.
    /// </summary>
    /// <param name="context">Контекст токенизации.</param>
    private static void CompleteUnclosedMultiLineComment(TokenizationContext context)
    {
        var commentText = context.MultiLineCommentBuilder.ToString();
        context.Exceptions.Add(
            new PlampException(
                PlampExceptionInfo.CommentIsNotClosed(),
                new FilePosition(context.MultiLineCommentStartOffset, commentText.Length, context.MultiLineCommentFileName)));
        context.Tokens.Add(
            new WhiteSpace(
                commentText,
                new FilePosition(context.MultiLineCommentStartOffset, commentText.Length, context.MultiLineCommentFileName),
                WhiteSpaceKind.MultiLineComment));
        context.MultiLineCommentBuilder.Clear();
        context.IsInsideMultiLineComment = false;
    }

    /// <summary>
    /// Пытается разобрать оператор
    /// </summary>
    /// <param name="text">Полный текст файла</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на первый символ после оператора</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="operator">Разобранный токен оператора</param>
    /// <returns><see langword="true"/>, если оператор успешно распознан; иначе <see langword="false"/>.</returns>
    private static bool TryParseOperator(
        string text,
        ref int position,
        int byteOffset,
        string fileName,
        [NotNullWhen(true)] out TokenBase? @operator)
    {
        if (text.Length - position >= 2)
        {
            var op = text[position..(position + 2)];
            var opPos = new FilePosition(byteOffset, 2, fileName);
            position += 2;
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
                    @operator = new OperatorToken(op, opPos, op.ToOperator());
                    return true;
            }

            position -= 2;
        }

        switch (text[position])
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
                var opString = text[position].ToString();
                @operator = new OperatorToken(opString, new FilePosition(byteOffset, 1, fileName), opString.ToOperator());
                position++;
                return true;
            default:
                @operator = null;
                return false;
        }
    }

    #endregion
}