using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Compilation.Models;
using plamp.Alternative.Tokenization.Enums;
using plamp.Alternative.Tokenization.Token;

namespace plamp.Alternative.Tokenization;


public static class Tokenizer
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
    
    public static TokenizationResult Tokenize(SourceFile sourceFile)
    {
        var rows = sourceFile.SourceCode.Split('\n');
        var prepared = rows.Select(x => x.Replace("\t", "    ")).Select((t, i) => new Row(i, t));

        var tokenList = new List<TokenBase>();
        var exceptionList = new List<PlampException>();
        var context = new TokenizationContext(sourceFile.FileName, tokenList, exceptionList);
        
        foreach (var row in prepared)
        {
            TokenizeSingleRow(row, context);
        }

        var endRow = rows.Length == 0 ? rows.Length : rows.Length - 1;
        var endColumns = rows.Length == 0 ? 0 : rows[endRow].Length;
        var pos = new FilePosition(endRow, endColumns);
        context.Tokens.Add(new EndOfFile(pos, pos));   
        
        return new TokenizationResult(new TokenSequence(context.Tokens), context.Exceptions);
    }
    
    private static void TokenizeSingleRow(Row row, TokenizationContext context)
    {
        for(var i = 0; i < row.Value.Length;)
        {
            if (char.IsLetter(row[i])) context.Tokens.Add(ParseWord(row, ref i, context));
            else if (char.IsDigit(row[i])) context.Tokens.Add(ParseNumber(row, ref i, context));
            else if (row[i] == '"')
            {
                var literal = ParseStringLiteral(row, ref i, context);
                context.Tokens.Add(literal);
            }
            else
            {
                if (!TryParseCustom(row, ref i, out var result, context)) continue;
                if(result != null) context.Tokens.Add(result);
            }
        }
    }

    #region Words

    private static TokenBase ParseWord(Row row, ref int position, TokenizationContext _)
    {
        var builder = new StringBuilder();
        var startPosition = new FilePosition(row.Number, position);
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
        
        var endPosition = new FilePosition(row.Number, position - 1);
        var word = builder.ToString();
        
        if (word.ToKeyword() != Keywords.Unknown)
        {
            return new KeywordToken(word, startPosition, endPosition, word.ToKeyword());
        }
        return new Word(word, startPosition, endPosition);
    }

    #endregion
    
    #region Numbers

    private static Literal ParseNumber(Row row, ref int position, TokenizationContext context)
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
            context.Exceptions.Add(new PlampException(PlampExceptionInfo.UnknownNumberFormat(), startPosition, end, context.FileName));
        }
        var (value, type) = cort;
        return new Literal(numberPart + postfix, startPosition, end, value, type!);
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

    private static Literal ParseStringLiteral(Row row, ref int position, TokenizationContext context)
    {
        var startPosition = new FilePosition(row.Number, position);
        var builder = new StringBuilder();
        position++;
        for (; position < row.Length; position++)
        {
            switch (row[position])
            {
                case '"':
                    var literal = new Literal($"\"{builder}\"", startPosition, new FilePosition(row.Number, position), builder.ToString(), typeof(string));
                    position++;
                    return literal;
                case '\\':
                    position++;
                    TryParseEscapedSequence(row, ref position, builder, context);
                    break;
                default:
                    builder.Append(row[position]);
                    break;
            }
        }

        var endPos = new FilePosition(row.Number, position - 1);
        context.Exceptions.Add(new PlampException(PlampExceptionInfo.StringIsNotClosed(), startPosition, endPos, context.FileName));
        return new Literal($"\"{builder}", startPosition, endPos, builder.ToString(), typeof(string));
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
                context.Exceptions.Add(new PlampException(PlampExceptionInfo.InvalidEscapeSequence($"\\{row[position]}"),
                    new FilePosition(row.Number, position - 1), new FilePosition(row.Number, position), context.FileName));
                return;
        }
    }

    #endregion

    #region Custom

    private static bool TryParseCustom(Row row, ref int position, out TokenBase? result, TokenizationContext context)
    {
        result = null;
        var startPosition = new FilePosition(row.Number, position);
        var endPos = new FilePosition(row.Number, position);
        switch (row[position])
        {
            case '{':
                result = new OpenCurlyBracket(startPosition, endPos);
                position++;
                return true;
            case '}':
                result = new CloseCurlyBracket(startPosition, endPos);
                position++;
                return true;
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
            case ';':
                result = new EndOfStatement(startPosition, endPos);
                position++;
                return true;
            case ' ':
            case '\r':
                result = new WhiteSpace(" ", startPosition, endPos, WhiteSpaceKind.WhiteSpace);
                position++;
                return true;
            case '\t':
                var endPosition = startPosition with { Column = startPosition.Column + 4 };
                result = new WhiteSpace("    ", startPosition, endPosition, WhiteSpaceKind.WhiteSpace);
                position++;
                return true;
            default:
                if (TryParseOperator(row, ref position, out var @operator))
                {
                    result = @operator;
                    return true;
                }
                context.Exceptions.Add(new PlampException(PlampExceptionInfo.UnexpectedToken(row[position].ToString()), 
                    startPosition, startPosition, context.FileName));
                position++;
                return false;
        }
    }
    
    private static bool TryParseOperator(Row row, ref int position, out TokenBase? @operator)
    {
        var startPosition = new FilePosition(row.Number, position);
        if (row.Length - position >= 2)
        {
            var op = row[position..(position+2)];
            var opEnd = new FilePosition(row.Number, position + 1);
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
                    @operator = new OperatorToken(op, startPosition, opEnd, operatorType);
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
                @operator = new OperatorToken(opString, startPosition, startPosition, opString.ToOperator());
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