using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using plamp.Native.Tokenization.Enumerations;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Tokenization;

public static partial class PlampNativeTokenizer
{
    private readonly record struct Row(int Number, string Value) : IEnumerable<char>
    {
        public int Length => Value.Length;
        public char this[int index] => Value[index];

        public string this[Range index] => Value[index];
        
        public IEnumerator<char> GetEnumerator()
        {
            return Value.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private const string EndOfLine = "\n";
    public const string EndOfLineCrlf = "\r\n";
    
    [GeneratedRegex($"(?:{EndOfLineCrlf}|{EndOfLine})")]
    private static partial Regex EndOfLineRegex();
    
    public static TokenizationResult Tokenize(this string code)
    {
        if (code == null)
        {
            return new TokenizationResult(new TokenSequence([new EndOfLine(EndOfLineCrlf, new(0, 0), new(0, 1))]), []);
        }
        
        var rows = EndOfLineRegex().Split(code);
        var prepared = rows.Select((t, i) => new Row(i, t));

        var tokenList = new List<TokenBase>();
        var exceptionList = new List<PlampException>();
        
        foreach (var row in prepared)
        {
            TokenizeSingleRow(row, tokenList, exceptionList);
        }
        
        return new TokenizationResult(new TokenSequence(tokenList), exceptionList);
    }

    private static void TokenizeSingleRow(Row row, List<TokenBase> tokenList, List<PlampException> exceptionList)
    {
        for(var i = 0; i < row.Value.Length;)
        {
            if (char.IsLetter(row[i]))
            {
                tokenList.Add(ParseWord(row, ref i));
            }
            else if(char.IsDigit(row[i]))
            {
                tokenList.Add(ParseNumber(row, ref i, exceptionList));                
            }
            else if (row[i] == '"')
            {
                var literal = ParseStringLiteral(row, ref i, exceptionList);
                if (literal != null)
                {
                    tokenList.Add(literal);
                }
            }
            else
            {
                if (TryParseCustom(row, ref i, out var result, exceptionList))
                {
                    tokenList.Add(result);
                }
            }
        }
        
        //Похоже на костыль
        var start = new TokenPosition(row.Number, row.Length);
        var end = new TokenPosition(row.Number, row.Length + EndOfLineCrlf.Length - 1);
        tokenList.Add(new EndOfLine(EndOfLineCrlf, start, end));    
    }

    private static NumberLiteral ParseNumber(Row row, ref int position, List<PlampException> exceptions)
    {
        var builder = new StringBuilder();
        var startPosition = new TokenPosition(row.Number, position);
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
        var end = new TokenPosition(row.Number, position - 1);
        if (!TryParseNumberTypePostfix(numberPart, postfix, out var cort))
        {
            exceptions.Add(new PlampException(PlampNativeExceptionInfo.UnknownNumberFormat, startPosition, end));
        }
        var (value, type) = cort;
        return new NumberLiteral(numberPart + postfix, startPosition, end, value, type);
    }

    private static bool TryParseNumberTypePostfix(string value, string postfix, out (object, Type) result)
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
            case "sb":
                res = sbyte.TryParse(value, CultureInfo.InvariantCulture, out var p);
                result = (p, typeof(sbyte));
                return res;
            case "s":
                res = short.TryParse(value, CultureInfo.InvariantCulture, out var q);
                result = (q, typeof(short));
                return res;
            case "us":
                res = ushort.TryParse(value, CultureInfo.InvariantCulture, out var r);
                result = (r, typeof(ushort));
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
    
    private static TokenBase ParseWord(Row row, ref int position)
    {
        var builder = new StringBuilder();
        var startPosition = new TokenPosition(row.Number, position);
        do
        {
            if (char.IsLetterOrDigit(row[position]))
            {
                builder.Append(row[position]);
                position++;
            }
            else
            {
                break;
            }
        } while (position < row.Length);
        
        var endPosition = new TokenPosition(row.Number, position - 1);
        var word = builder.ToString();
        
        if (word.ToKeyword() != Keywords.Unknown)
        {
            return new KeywordToken(word, startPosition, endPosition, word.ToKeyword());
        }
        return new Word(word, startPosition, endPosition);
    }

    private static StringLiteral ParseStringLiteral(Row row, ref int position, List<PlampException> exceptions)
    {
        var startPosition = new TokenPosition(row.Number, position);
        var builder = new StringBuilder();
        position++;
        for (; position < row.Length; position++)
        {
            switch (row[position])
            {
                case '\n':
                    var end = new TokenPosition(row.Number, position - 1);
                    var literal = new StringLiteral(builder.ToString(), startPosition, end); 
                    exceptions.Add(new PlampException(PlampNativeExceptionInfo.StringIsNotClosed(), startPosition, end));
                    return literal;
                case '"':
                    literal = new StringLiteral(builder.ToString(), startPosition, new TokenPosition(row.Number, position));
                    position++;
                    return literal;
                case '\\':
                    position++;
                    TryParseEscapedSequence(row, ref position, builder, exceptions);
                    break;
                case '\r':
                case '\t':
                    if (row.Length < position + 1 && row[position] == EndOfLineCrlf[0] && row[position + 1] == EndOfLineCrlf[1])
                    {
                        var endPosition = new TokenPosition(row.Number, position - 1);
                        exceptions.Add(new PlampException(PlampNativeExceptionInfo.StringIsNotClosed(), startPosition, endPosition));
                        literal = new StringLiteral(builder.ToString(), startPosition, endPosition);
                        return literal;
                    }
                    
                    exceptions.Add(new PlampException(PlampNativeExceptionInfo.StringIsNotClosed(),
                        new TokenPosition(row.Number, position), new TokenPosition(row.Number, position)));
                    break;
                default:
                    builder.Append(row[position]);
                    break;
            }
        }

        var endPos = new TokenPosition(row.Number, position - 1);
        exceptions.Add(new PlampException(PlampNativeExceptionInfo.StringIsNotClosed(), startPosition, endPos));
        return new StringLiteral(builder.ToString(), startPosition, endPos);
    }

    private static void TryParseEscapedSequence(Row row, ref int position, StringBuilder builder,
        List<PlampException> exceptions)
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
                exceptions.Add(new PlampException(PlampNativeExceptionInfo.InvalidEscapeSequence($"\\{row[position]}"),
                    new TokenPosition(row.Number, position - 1), new TokenPosition(row.Number, position)));
                return;
        }
    }
    
    private static bool TryParseCustom(Row row, ref int position, out TokenBase result, List<PlampException> exceptions)
    {
        result = null;
        var startPosition = new TokenPosition(row.Number, position);
        if (row.Length > position + 3 && row[position..(position + 4)] == "    ")
        {
            position += 4;
            result = new WhiteSpace("\t", startPosition, new TokenPosition(row.Number, position - 1),
                WhiteSpaceKind.Scope);
            return true;
        }

        var endPos = new TokenPosition(row.Number, position);
        switch (row[position])
        {
            case '[':
                result = new OpenSquareBracket(startPosition, endPos);
                position++;
                return true;
            case ']':
                result = new CloseSquareBracket(startPosition, endPos);
                position++;
                return true;
            case '(':
                result = new OpenParen(startPosition, endPos);
                position++;
                return true;
            case ')':
                result = new CloseParen(startPosition, endPos);
                position++;
                return true;
            case ',':
                result = new Comma(startPosition, endPos);
                position++;
                return true;
            case '\t':
                result = new WhiteSpace("\t", startPosition, endPos, WhiteSpaceKind.Scope);
                position++;
                return true;
            //Not possible
            case '\n':
                result = new EndOfLine(EndOfLine, startPosition, endPos);
                position++;
                return true;
            case ' ':
                result = new WhiteSpace(" ", startPosition, endPos, WhiteSpaceKind.WhiteSpace);
                position++;
                return true;
            case '\r':
                if (row.Length > position + 1 && row[position..(position + 2)] == EndOfLineCrlf)
                {
                    position += 2;
                    endPos = new TokenPosition(row.Number, position - 1);
                    result = new EndOfLine(EndOfLineCrlf, startPosition, endPos);
                    return true;
                }
                position++;
                result = new WhiteSpace("\r", startPosition, endPos, WhiteSpaceKind.WhiteSpace);
                return true;
            default:
                if (TryParseOperator(row, ref position, out var @operator))
                {
                    result = @operator;
                    return true;
                }
                exceptions.Add(new PlampException(PlampNativeExceptionInfo.UnexpectedToken(row[position].ToString()), 
                    startPosition, startPosition));
                position++;
                return false;
        }
    }
    
    private static bool TryParseOperator(Row row, ref int position, out TokenBase @operator)
    {
        var startPosition = new TokenPosition(row.Number, position);
        if (row.Length - position >= 2)
        {
            var op = row[position..(position+2)];
            var opEnd = new TokenPosition(row.Number, position + 1);
            position += 2;
            switch (op)
            {
                case "+=":
                case "-=":
                case "++":
                case "--":
                case "*=":
                case "/=":
                case "==":
                case "!=":
                case "<=":
                case ">=":
                case "&&":
                case "||":
                case "%=":
                case "&=":
                case "|=":
                case "^=":
                    var operatorType = op.ToOperator();
                    @operator = new OperatorToken(op, startPosition, opEnd, operatorType);
                    return true;
                case "->":
                    @operator = new LineBreak("->", startPosition, opEnd);
                    return true;
            }

            position -= 2;
        }

        switch (row[position])
        {
            case '+':
            case '-':
            case '=':
            case '.':
            case '/':
            case '*':
            case '!':
            case '%':
            case '|':
            case '&':
            case '^':
                var opString = row[position].ToString();
                @operator = new OperatorToken(opString, startPosition, startPosition, opString.ToOperator());
                break;
            case '<':
                opString = row[position].ToString();
                @operator = new OpenAngleBracket(opString, startPosition, startPosition);
                break;
            case '>':
                opString = row[position].ToString();
                @operator = new CloseAngleBracket(opString, startPosition, startPosition);
                break;
            default:
                @operator = null;
                return false;
        }

        position++;
        return true;
    }
}