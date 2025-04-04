using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Compilation;
using plamp.Native.Tokenization.Enumerations;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Tokenization;

internal static partial class PlampNativeTokenizer
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
    
    internal static TokenizationResult Tokenize(this SourceFile sourceFile, AssemblyName assemblyName)
    {
        if (sourceFile.SourceCode == null)
        {
            return new TokenizationResult(new TokenSequence([new EndOfLine(EndOfLineCrlf, new(0, 0), new(0, 1))]), []);
        }
        
        var rows = EndOfLineRegex().Split(sourceFile.SourceCode);
        var prepared = rows.Select((t, i) => new Row(i, t));

        var tokenList = new List<TokenBase>();
        var exceptionList = new List<PlampException>();
        var context = new TokenizationContext(sourceFile.FileName, rows, tokenList, exceptionList, assemblyName);
        
        foreach (var row in prepared)
        {
            TokenizeSingleRow(row, context);
        }
        
        return new TokenizationResult(new TokenSequence(context.Tokens), context.Exceptions);
    }

    private static void TokenizeSingleRow(Row row, TokenizationContext context)
    {
        for(var i = 0; i < row.Value.Length;)
        {
            if (char.IsLetter(row[i]))
            {
                context.Tokens.Add(ParseWord(row, ref i, context));
            }
            else if(char.IsDigit(row[i]))
            {
                context.Tokens.Add(ParseNumber(row, ref i, context));                
            }
            else if (row[i] == '"')
            {
                var literal = ParseStringLiteral(row, ref i, context);
                if (literal != null)
                {
                    context.Tokens.Add(literal);
                }
            }
            else
            {
                if (TryParseCustom(row, ref i, out var result, context))
                {
                    context.Tokens.Add(result);
                }
            }
        }
        
        //Похоже на костыль
        var start = new FilePosition(row.Number, row.Length);
        var end = new FilePosition(row.Number, row.Length + EndOfLineCrlf.Length - 1);
        context.Tokens.Add(new EndOfLine(EndOfLineCrlf, start, end));    
    }

    private static NumberLiteral ParseNumber(Row row, ref int position, TokenizationContext context)
    {
        var builder = new StringBuilder();
        var startPosition = new FilePosition(row.Number, position);
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
        var end = new FilePosition(row.Number, position - 1);
        if (!TryParseNumberTypePostfix(numberPart, postfix, out var cort))
        {
            context.Exceptions.Add(new PlampException(PlampNativeExceptionInfo.UnknownNumberFormat, startPosition, end, context.FileName, context.AssemblyName));
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
    
    private static TokenBase ParseWord(Row row, ref int position, TokenizationContext context)
    {
        var builder = new StringBuilder();
        var startPosition = new FilePosition(row.Number, position);
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
        
        var endPosition = new FilePosition(row.Number, position - 1);
        var word = builder.ToString();
        
        if (word.ToKeyword() != Keywords.Unknown)
        {
            return new KeywordToken(word, startPosition, endPosition, word.ToKeyword());
        }
        return new Word(word, startPosition, endPosition);
    }

    private static StringLiteral ParseStringLiteral(Row row, ref int position, TokenizationContext context)
    {
        var startPosition = new FilePosition(row.Number, position);
        var builder = new StringBuilder();
        position++;
        for (; position < row.Length; position++)
        {
            switch (row[position])
            {
                case '\n':
                    var end = new FilePosition(row.Number, position - 1);
                    var literal = new StringLiteral(builder.ToString(), startPosition, end); 
                    context.Exceptions.Add(new PlampException(PlampNativeExceptionInfo.StringIsNotClosed(), startPosition, end, context.FileName, context.AssemblyName));
                    return literal;
                case '"':
                    literal = new StringLiteral(builder.ToString(), startPosition, new FilePosition(row.Number, position));
                    position++;
                    return literal;
                case '\\':
                    position++;
                    TryParseEscapedSequence(row, ref position, builder, context);
                    break;
                case '\r':
                case '\t':
                    if (row.Length < position + 1 && row[position] == EndOfLineCrlf[0] && row[position + 1] == EndOfLineCrlf[1])
                    {
                        var endPosition = new FilePosition(row.Number, position - 1);
                        context.Exceptions.Add(new PlampException(PlampNativeExceptionInfo.StringIsNotClosed(), startPosition, endPosition, context.FileName, context.AssemblyName));
                        literal = new StringLiteral(builder.ToString(), startPosition, endPosition);
                        return literal;
                    }
                    
                    context.Exceptions.Add(new PlampException(PlampNativeExceptionInfo.StringIsNotClosed(),
                        new FilePosition(row.Number, position), new FilePosition(row.Number, position), context.FileName, context.AssemblyName));
                    break;
                default:
                    builder.Append(row[position]);
                    break;
            }
        }

        var endPos = new FilePosition(row.Number, position - 1);
        context.Exceptions.Add(new PlampException(PlampNativeExceptionInfo.StringIsNotClosed(), startPosition, endPos, context.FileName, context.AssemblyName));
        return new StringLiteral(builder.ToString(), startPosition, endPos);
    }

    private static void TryParseEscapedSequence(Row row, ref int position, StringBuilder builder,
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
                context.Exceptions.Add(new PlampException(PlampNativeExceptionInfo.InvalidEscapeSequence($"\\{row[position]}"),
                    new FilePosition(row.Number, position - 1), new FilePosition(row.Number, position), context.FileName, context.AssemblyName));
                return;
        }
    }
    
    private static bool TryParseCustom(Row row, ref int position, out TokenBase result, TokenizationContext context)
    {
        result = null;
        var startPosition = new FilePosition(row.Number, position);
        if (row.Length > position + 3 && row[position..(position + 4)] == "    ")
        {
            position += 4;
            result = new WhiteSpace("\t", startPosition, new FilePosition(row.Number, position - 1),
                WhiteSpaceKind.Scope);
            return true;
        }

        var endPos = new FilePosition(row.Number, position);
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
                    endPos = new FilePosition(row.Number, position - 1);
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
                context.Exceptions.Add(new PlampException(PlampNativeExceptionInfo.UnexpectedToken(row[position].ToString()), 
                    startPosition, startPosition, context.FileName, context.AssemblyName));
                position++;
                return false;
        }
    }
    
    private static bool TryParseOperator(Row row, ref int position, out TokenBase @operator)
    {
        var startPosition = new FilePosition(row.Number, position);
        if (row.Length - position >= 2)
        {
            var op = row[position..(position+2)];
            var opEnd = new FilePosition(row.Number, position + 1);
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