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
        var encoding = fileReader.CurrentEncoding;
        var content = await fileReader.ReadToEndAsync(token);
        content = NormalizeLineEndings(content);

        var byteOffset = 0;
        for (var i = 0; i < content.Length;)
        {
            var prevIx = i;
            if (char.IsLetter(content[i]))
            {
                context.Tokens.Add(ParseWord(content, ref i, byteOffset, fileName, context));
            }
            else if (char.IsDigit(content[i]))
            {
                context.Tokens.Add(ParseNumber(content, ref i, byteOffset, fileName, context));
            }
            else if (content[i] == '"')
            {
                var literal = ParseStringLiteral(content, ref i, byteOffset, fileName, encoding, context);
                context.Tokens.Add(literal);
            }
            else if (TryParseCustom(content, ref i, byteOffset, fileName, out var result, context))
            {
                context.Tokens.Add(result);
            }

            byteOffset += encoding.GetByteCount(content.AsSpan().Slice(prevIx, i - prevIx));
        }

        // Парсер ожидает перевод строки в конце файла так же, как и при старой построчной токенизации
        if (content.Length > 0 && content[^1] != '\n')
        {
            context.Tokens.Add(new WhiteSpace("\n", new FilePosition(byteOffset, 1, fileName), WhiteSpaceKind.LineBreak));
            byteOffset += encoding.GetByteCount("\n");
        }

        var pos = new FilePosition(byteOffset, 0, fileName);
        context.Tokens.Add(new EndOfFile(pos));

        return new TokenizationResult(new TokenSequence(context.Tokens), context.Exceptions);
    }

    /// <summary>
    /// Приводит все варианты перевода строки к виду <c>\n</c>,
    /// чтобы токенизация и расчёт позиций работали одинаково
    /// </summary>
    /// <param name="content">Исходный текст файла</param>
    /// <returns>Текст с нормализованными переводами строк</returns>
    private static string NormalizeLineEndings(string content) => content.Replace("\r\n", "\n").Replace('\r', '\n');

    #region Words

    /// <summary>
    /// Разбирает идентификатор или ключевое слово, начиная с текущей позиции
    /// </summary>
    /// <param name="text">Полный текст файла</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на первый символ после слова</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="context">Контекст токенизации</param>
    /// <returns>Токен ключевого слова или идентификатора</returns>
    private static TokenBase ParseWord(string text, ref int position, int byteOffset, string fileName, TokenizationContext context)
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
    /// <param name="text">Полный текст файла</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на первый символ после литерала</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="context">Контекст токенизации</param>
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
    /// <param name="text">Полный текст файла</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на первый символ после литерала</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="fileEncoding">Кодировка файла</param>
    /// <param name="context">Контекст токенизации</param>
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
                    var literal = new Literal(
                        $"\"{builder}\"",
                        new FilePosition(byteOffset, position - start, fileName),
                        builder.ToString(),
                        Builtins.String);
                    return literal;
                case '\\':
                    position++;
                    if (position >= text.Length)
                    {
                        position = text.Length;
                        break;
                    }

                    TryParseEscapedSequence(text, ref position, byteOffset, fileName, fileEncoding, builder, context);
                    break;
                case '\n':
                    var filePosition = new FilePosition(byteOffset, position - start, fileName);
                    context.Exceptions.Add(new PlampException(PlampExceptionInfo.StringIsNotClosed(), filePosition));
                    return new Literal($"\"{builder}", filePosition, builder.ToString(), Builtins.String);
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
    /// <param name="text">Полный текст файла</param>
    /// <param name="position">Позиция символа сразу после обратного слеша</param>
    /// <param name="byteOffset">Смещение начала строкового литерала в байтах</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="fileEncoding">Кодировка файла, используемая для вычисления смещений в диагностике</param>
    /// <param name="builder">Накопитель результирующего строкового значения</param>
    /// <param name="context">Контекст токенизации</param>
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
    /// <param name="text">Полный текст файла</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на первый символ после разобранного токена</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="result">Разобранный токен</param>
    /// <param name="context">Контекст токенизации для накопления ошибок</param>
    /// <returns><see langword="true"/>, если токен был распознан; иначе <see langword="false"/></returns>
    private static bool TryParseCustom(
        string text,
        ref int position,
        long byteOffset,
        string fileName,
        [NotNullWhen(true)] out TokenBase? result,
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
            case '\n':
                result = new WhiteSpace("\n", filePosition, WhiteSpaceKind.LineBreak);
                position++;
                return true;
            case ':' when next != '=':
                result = new Colon(filePosition);
                position++;
                return true;
            case '/' when next == '/':
                result = ParseSingleLineComment(text, ref position, byteOffset, fileName);
                return true;
            case '/' when next == '*':
                result = ParseMultiLineComment(text, ref position, byteOffset, fileName, context);
                return true;
            default:
                if (TryParseOperator(text, ref position, byteOffset, fileName, out var @operator))
                {
                    result = @operator;
                    return true;
                }

                context.Exceptions.Add(new PlampException(PlampExceptionInfo.UnexpectedToken(text[position].ToString()), filePosition));
                position++;
                return false;
        }
    }

    /// <summary>
    /// Разбирает однострочный комментарий до конца текущей строки
    /// </summary>
    /// <param name="text">Полный текст файла</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на перевод строки или конец файла</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    /// <param name="fileName">Имя файла</param>
    /// <returns>Токен trivia для однострочного комментария</returns>
    private static WhiteSpace ParseSingleLineComment(string text, ref int position, long byteOffset, string fileName)
    {
        var start = position;
        position += 2;
        while (position < text.Length && text[position] != '\n')
        {
            position++;
        }

        var comment = text[start..position];
        return new WhiteSpace(comment, new FilePosition(byteOffset, comment.Length, fileName), WhiteSpaceKind.SingleLineComment);
    }

    /// <summary>
    /// Разбирает многострочный комментарий до закрывающей последовательности <c>*/</c>
    /// </summary>
    /// <param name="text">Полный текст файла</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на первый символ после комментария или на конец файла</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="context">Контекст токенизации</param>
    /// <returns>Токен trivia для многострочного комментария</returns>
    private static WhiteSpace ParseMultiLineComment(
        string text,
        ref int position,
        long byteOffset,
        string fileName,
        TokenizationContext context)
    {
        var start = position;
        position += 2;

        // Комментарий должен сохраниться целиком, поэтому сканируем текст до точной пары */.
        while (position + 1 < text.Length)
        {
            if (text[position] == '*' && text[position + 1] == '/')
            {
                position += 2;
                var closedComment = text[start..position];
                return new WhiteSpace(
                    closedComment,
                    new FilePosition(byteOffset, closedComment.Length, fileName),
                    WhiteSpaceKind.MultiLineComment);
            }

            position++;
        }

        position = text.Length;
        var unclosedComment = text[start..position];
        context.Exceptions.Add(
            new PlampException(
                PlampExceptionInfo.CommentIsNotClosed(),
                new FilePosition(byteOffset, unclosedComment.Length, fileName)));
        return new WhiteSpace(
            unclosedComment,
            new FilePosition(byteOffset, unclosedComment.Length, fileName),
            WhiteSpaceKind.MultiLineComment);
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
        long byteOffset,
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
                    var operatorType = op.ToOperator();
                    @operator = new OperatorToken(op, opPos, operatorType);
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
                var opPos = new FilePosition(byteOffset, 1, fileName);
                @operator = new OperatorToken(opString, opPos, opString.ToOperator());
                break;
            default:
                @operator = null;
                return false;
        }

        position++;
        return true;
    }

    #endregion
}