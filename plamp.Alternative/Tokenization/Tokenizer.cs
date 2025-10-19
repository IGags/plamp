using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization.Enums;
using plamp.Alternative.Tokenization.Token;

namespace plamp.Alternative.Tokenization;


public static class Tokenizer
{
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
        
        while(await fileReader.ReadLineAsync(token) is { } line)
        {
            for (var i = 0; i < line.Length;)
            {
                var prevIx = i;
                if (char.IsLetter(line[i])) context.Tokens.Add(ParseWord(line, ref i, byteOffset, fileName, context));
                else if (char.IsDigit(line[i])) context.Tokens.Add(ParseNumber(line, ref i, byteOffset, fileName, context));
                else if (line[i] == '"')
                {
                    var literal = ParseStringLiteral(line, ref i, byteOffset, fileName, encoding, context);
                    context.Tokens.Add(literal);
                }
                else
                {
                    if (TryParseCustom(line, ref i, byteOffset, fileName, out var result, context)) context.Tokens.Add(result);
                }

                byteOffset += encoding.GetByteCount(line.AsSpan().Slice(prevIx, i - prevIx));
            }
            context.Tokens.Add(new WhiteSpace("\n", new FilePosition(byteOffset, 1, fileName), WhiteSpaceKind.LineBreak));
            byteOffset += encoding.GetByteCount("\n");
        }

        var pos = new FilePosition(byteOffset, 0, fileName);
        context.Tokens.Add(new EndOfFile(pos));   
        
        return new TokenizationResult(new TokenSequence(context.Tokens), context.Exceptions);
    }

    #region Words

    private static TokenBase ParseWord(string row, ref int position, int byteOffset, string fileName, TokenizationContext _)
    {
        var builder = new StringBuilder();
        do
        {
            if (char.IsLetterOrDigit(row[position]) || row[position] == '_')
            {
                builder.Append(row[position]);
                position++;
            }
            else
            {
                break;
            }
        } while (position < row.Length);
        
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

    private static Literal ParseNumber(string row, ref int position, int byteOffset, string fileName, TokenizationContext context)
    {
        var builder = new StringBuilder();
        var isFractional = false;
        do
        {
            if (char.IsDigit(row[position]))
            {
                builder.Append(row[position]);
                position++;
            }
            else if(!isFractional && row[position] == '.' 
                                  && position + 1 < row.Length 
                                  && char.IsDigit(row[position + 1]))
            {
                isFractional = true;
                builder.Append(row[position]);
                position++;
            }
            else
            {
                break;
            }
        } while (position < row.Length);
        
        var postfixBuilder = new StringBuilder();
        while (position < row.Length)
        {
            if (char.IsLetter(row[position]))
            {
                postfixBuilder.Append(row[position]);
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

    private static bool TryParseNumberTypePostfix(string value, string postfix, out (object, Type?) result)
    {
        switch (postfix)
        {
            case "i":
                var res = int.TryParse(value, CultureInfo.InvariantCulture, out var i);
                result = (i, typeof(int));
                return res;
            case "ui":
                res = uint.TryParse(value, CultureInfo.InvariantCulture, out var j);
                result = (j, typeof(uint));
                return res;
            case "l":
                res = long.TryParse(value, CultureInfo.InvariantCulture, out var k);
                result = (k, typeof(long));
                return res;
            case "ul":
                res = ulong.TryParse(value, CultureInfo.InvariantCulture, out var l);
                result = (l, typeof(ulong));
                return res;
            case "d":
                res = double.TryParse(value, CultureInfo.InvariantCulture, out var m);
                result = (m, typeof(double));
                return res;
            case "f":
                res = float.TryParse(value, CultureInfo.InvariantCulture, out var n);
                result = (n, typeof(float));
                return res;
            case "b":
                res = byte.TryParse(value, CultureInfo.InvariantCulture, out var o);
                result = (o, typeof(byte));
                return res;
            case "":
                if (value.Contains('.'))
                {
                    res = double.TryParse(value, CultureInfo.InvariantCulture, out var s);
                    result = (s, typeof(double));
                    return res;
                }

                res = long.TryParse(value, CultureInfo.InvariantCulture, out var t);
                if (res)
                {
                    if (t is <= int.MaxValue and >= int.MinValue)
                    {
                        result = ((int)t, typeof(int));
                        return true;
                    }

                    result = (t, typeof(long));
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

    private static Literal ParseStringLiteral(string row, ref int position, int byteOffset, string fileName, Encoding fileEncoding, TokenizationContext context)
    {
        var builder = new StringBuilder();
        var start = position;
        position++;
        for (; position < row.Length; position++)
        {
            switch (row[position])
            {
                case '"':
                    position++;
                    var literal = new Literal($"\"{builder}\"", new FilePosition(byteOffset, position - start, fileName), builder.ToString(), typeof(string));
                    return literal;
                case '\\':
                    position++;
                    TryParseEscapedSequence(row, ref position, byteOffset, fileName, fileEncoding, builder, context);
                    break;
                default:
                    builder.Append(row[position]);
                    break;
            }
        }

        var filePosition = new FilePosition(byteOffset, position - start, fileName);
        context.Exceptions.Add(new PlampException(PlampExceptionInfo.StringIsNotClosed(), filePosition));
        return new Literal($"\"{builder}", filePosition, builder.ToString(), typeof(string));
    }
    
    private static void TryParseEscapedSequence(
        string row, 
        ref int position, 
        int byteOffset, 
        string fileName,
        Encoding fileEncoding,
        StringBuilder builder,
        TokenizationContext context)
    {
        switch (row[position])
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
                    new PlampException(PlampExceptionInfo.InvalidEscapeSequence($"\\{row[position]}"), 
                        new FilePosition(byteOffset + fileEncoding.GetByteCount(builder.ToString()) + fileEncoding.GetByteCount("\""), 2, fileName)));
                return;
        }
    }

    #endregion

    #region Custom

    private static bool TryParseCustom(
        string row,
        ref int position,
        long byteOffset,
        string fileName,
        [NotNullWhen(true)] out TokenBase? result,
        TokenizationContext context)
    {
        result = null;
        var filePosition = new FilePosition(byteOffset, 1, fileName);
        char? next = position + 1 < row.Length ? row[position + 1] : null;
        switch (row[position])
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
            default:
                if (TryParseOperator(row, ref position, byteOffset, fileName, out var @operator))
                {
                    result = @operator;
                    return true;
                }
                context.Exceptions.Add(new PlampException(PlampExceptionInfo.UnexpectedToken(row[position].ToString()), filePosition));
                position++;
                return false;
        }
    }
    
    private static bool TryParseOperator(
        string row,
        ref int position,
        long byteOffset,
        string fileName,
        [NotNullWhen(true)] out TokenBase? @operator)
    {
        if (row.Length - position >= 2)
        {
            var op = row[position..(position+2)];
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

        switch (row[position])
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
                var opString = row[position].ToString();
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