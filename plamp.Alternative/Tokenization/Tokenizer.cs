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
        var byteOffset = 0L;
        var encoding = fileReader.CurrentEncoding;

        while (await fileReader.ReadLineAsync(token) is { } line)
        {
            for (var i = 0; i < line.Length;)
            {
                var prevIx = i;
                if (TryParseSingleLineComment(line, ref i, byteOffset, fileName, out var comment)) context.Tokens.Add(comment);
                else if (TryParseMultilineComment(ref line, fileReader, ref i, ref byteOffset, encoding, fileName, context, out comment))
                {
                    context.Tokens.Add(comment);
                    continue;
                }
                else if (char.IsLetter(line[i])) context.Tokens.Add(ParseWord(line, ref i, byteOffset, fileName));
                else if (char.IsDigit(line[i])) context.Tokens.Add(ParseNumber(line, ref i, byteOffset, fileName, context));
                else if (line[i] == '"') context.Tokens.Add(ParseStringLiteral(line, ref i, byteOffset, fileName, encoding, context));
                else if (line[i] == '\'') context.Tokens.Add(ParseCharLiteral(line, ref i, byteOffset, fileName, encoding, context));
                else if (TryParseCustom(line, ref i, byteOffset, fileName, out var result, context) && result != null)
                {
                    context.Tokens.Add(result);
                }
                
                byteOffset += encoding.GetByteCount(line.AsSpan().Slice(prevIx, i - prevIx));
            }


            context.Tokens.Add(new WhiteSpace("\n", new FilePosition(byteOffset, 1, fileName), WhiteSpaceKind.LineBreak));
            byteOffset += encoding.GetByteCount("\n");
        }

        context.Tokens.Add(new EndOfFile(new FilePosition(byteOffset, 0, fileName)));
        var sequence = new TokenSequence(context.Tokens);

        return new TokenizationResult(sequence, context.Exceptions);
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
    private static TokenBase ParseWord(string text, ref int position, long byteOffset, string fileName)
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
    private static Literal ParseNumber(string text, ref int position, long byteOffset, string fileName, TokenizationContext context)
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

    #region Chars

    /// <summary>
    /// Разбирает символьный литерал
    /// </summary>
    /// <param name="text">Текущая строка исходного файла</param>
    /// <param name="position">Текущая позиция чтения. После вызова указывает на первый символ после литерала</param>
    /// <param name="byteOffset">Смещение текущей позиции в байтах от начала файла</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="fileEncoding">Кодировка файла</param>
    /// <param name="context">Контекст токенизации для накопления ошибок</param>
    /// <returns>Токен символьного литерала</returns>
    private static Literal ParseCharLiteral(
        string text,
        ref int position,
        long byteOffset,
        string fileName,
        Encoding fileEncoding,
        TokenizationContext context)
    {
        var start = position;
        var valueBuilder = new StringBuilder();
        var hasInvalidContent = false;
        position++;

        for (; position < text.Length; position++)
        {
            switch (text[position])
            {
                case '\'':
                    position++;
                    var closedPosition = new FilePosition(byteOffset, position - start, fileName);
                    if (!hasInvalidContent && valueBuilder.Length != 1)
                    {
                        context.Exceptions.Add(new PlampException(PlampExceptionInfo.InvalidCharLiteral(), closedPosition));
                    }

                    return new Literal(text[start..position], closedPosition, valueBuilder.Length > 0 ? valueBuilder[0] : '\0', Builtins.Char);
                case '\\':
                    position++;
                    if (position >= text.Length)
                    {
                        position = text.Length;
                        var escapedEndPosition = new FilePosition(byteOffset, position - start, fileName);
                        context.Exceptions.Add(new PlampException(PlampExceptionInfo.CharIsNotClosed(), escapedEndPosition));
                        return new Literal(text[start..position], escapedEndPosition, valueBuilder.Length > 0 ? valueBuilder[0] : '\0', Builtins.Char);
                    }

                    var exceptionCount = context.Exceptions.Count;
                    TryParseEscapedSequence(text, ref position, byteOffset, fileName, fileEncoding, valueBuilder, context, true);
                    hasInvalidContent = hasInvalidContent || context.Exceptions.Count != exceptionCount;
                    break;
                default:
                    valueBuilder.Append(text[position]);
                    break;
            }
        }

        var endPosition = new FilePosition(byteOffset, position - start, fileName);
        context.Exceptions.Add(new PlampException(PlampExceptionInfo.CharIsNotClosed(), endPosition));
        return new Literal(text[start..position], endPosition, valueBuilder.Length > 0 ? valueBuilder[0] : '\0', Builtins.Char);
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
        long byteOffset,
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
    /// <param name="allowSingleQuote">Разрешает escape-последовательность одинарной кавычки для символьных литералов</param>
    private static void TryParseEscapedSequence(
        string text,
        ref int position,
        long byteOffset,
        string fileName,
        Encoding fileEncoding,
        StringBuilder builder,
        TokenizationContext context,
        bool allowSingleQuote = false)
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
            case '\'' when allowSingleQuote:
                builder.Append('\'');
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
    /// <param name="result">Разобранный токен</param>
    /// <param name="context">Контекст токенизации для накопления ошибок</param>
    /// <returns><see langword="true"/>, если токен был распознан; иначе <see langword="false"/></returns>
    private static bool TryParseCustom(
        string text,
        ref int position,
        long byteOffset,
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
            case '\r':
                result = new WhiteSpace(" ", filePosition, WhiteSpaceKind.WhiteSpace);
                position++;
                return true;
            case '\t':
                result = new WhiteSpace("\t", filePosition, WhiteSpaceKind.WhiteSpace);
                position++;
                return true;
            case ':' when next != '=':
                result = new Colon(filePosition);
                position++;
                return true;
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
    /// Парсинг комментария длиной в одну строку.
    /// </summary>
    /// <param name="text">Строка, которую надо распарсить</param>
    /// <param name="position">С какого места начинать парсить</param>
    /// <param name="byteOffset">Глобальное смещение в файле</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="token">Результат парсинга, не null, если true</param>
    /// <returns>Успешность операции парсинга</returns>
    private static bool TryParseSingleLineComment(
        string text, 
        ref int position,
        long byteOffset,
        string fileName,
        [NotNullWhen(true)]out WhiteSpace? token)
    {
        token = null;
        if (text.Length <= position + 1) return false;
        if (text[position] != '/' || text[position + 1] != '/') return false;

        var content = text[position..];
        var commentPos = new FilePosition(byteOffset, content.Length, fileName);
        token = new WhiteSpace(content, commentPos, WhiteSpaceKind.SingleLineComment);
        position += content.Length;
        return true;
    }

    /// <summary>
    /// Инкапсулирует всю логику парсинга комментариев, в случае true возвращает позицию сразу после комментария, из-за этого не требует перерасчёта byteOffset после себя.
    /// </summary>
    /// <param name="text">Текущая строка в которой происходит токенизация</param>
    /// <param name="next">Ридер, в случае если комментарий распространяется на несколько строк этот метод сам получает продолжение</param>
    /// <param name="position">Позиция старта</param>
    /// <param name="byteOffset">Смещение в исходном кодовом файле</param>
    /// <param name="encoding">Кодировка исходного файла</param>
    /// <param name="fileName">Имя исходного файла</param>
    /// <param name="context">Контекст, в который записываются возможные ошибки</param>
    /// <param name="token">Если результат true, то будет получен токен с комментарием.</param>
    /// <returns></returns>
    private static bool TryParseMultilineComment(
        ref string text,
        StreamReader next,
        ref int position,
        ref long byteOffset,
        Encoding encoding,
        string fileName,
        TokenizationContext context,
        [NotNullWhen(true)]out WhiteSpace? token)
    {
        token = null;
        int startIx;
        //Если не нашли в строке метку старта начиная со смещения, то комментария здесь нет. 
        if ((startIx = text.IndexOf("/*", position, StringComparison.InvariantCulture)) != position) return false;

        //Иначе запоминаем смещение начала комментария
        var startOffset = byteOffset;
        var commentBuilder = new StringBuilder();

        //Пропускаем метку старта.
        position += 2;
        int endIx;
        //Делаем логику в цикле, пока не найдём метку конца, каждая итерация этого цикла читает 1 новую строку
        while ((endIx = text.IndexOf("*/", position, StringComparison.InvariantCulture)) < 0)
        {
            //Забираем всё до конца строки так как не нашли метку конца в этой линии
            var read = text[startIx..];
            commentBuilder.Append(read);
            
            //Добавляем смещение
            byteOffset += encoding.GetByteCount(read);
            
            var readRes = next.ReadLine();
            //Логика обработки последней строки в файле
            if (readRes == null)
            {
                //Ставим максимальную позицию прошлой строки, чтобы выкинуло из внешнего цикла.
                position = text.Length;
                //Создаём токен и ошибку о том, что комментарий надо закрыть
                var errorPos = new FilePosition(startOffset, commentBuilder.Length, fileName);
                token = new WhiteSpace(commentBuilder.ToString(), errorPos, WhiteSpaceKind.MultiLineComment);
                context.Exceptions.Add(new PlampException(PlampExceptionInfo.CommentIsNotClosed(), errorPos));
                return true;
            }
            
            //Иначе добавляем перенос и перерассчитываем смещение
            commentBuilder.Append('\n');
            byteOffset += encoding.GetByteCount("\n");
            //Обновляем актуальную строку, с которой будем дальше работать
            text = readRes;
            //Ставим позицию в 0 для следующей строки
            position = 0;
        }

        //Если нашли метку, то находим следующий символ после неё
        position = endIx + 2;
        var commentPart = text[startIx..position];
        commentBuilder.Append(commentPart);
        //Перерассчитываем смещение и собираем готовый токен. 
        byteOffset += encoding.GetByteCount(commentPart);
        var commentPos = new FilePosition(startOffset, commentBuilder.Length, fileName);
        token = new WhiteSpace(commentBuilder.ToString(), commentPos, WhiteSpaceKind.MultiLineComment);
        return true;
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
