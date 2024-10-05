using System.Collections.Generic;
using System.Linq;
using System.Text;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Tokenization;

public static class PlampNativeTokenizer
{
    public const char EndOfLine = '\n';
    public const string EndOfLineCrlf = "\r\n";
    
    public static TokenizationResult Tokenize(this string code)
    {
        if (code == null)
        {
            return new TokenizationResult(new TokenSequence([]), []);
        }
        
        var tokenList = new List<TokenBase>();
        var exceptionList = new List<TokenizeException>();
        for(var i = 0; i < code.Length;)
        {
            if (char.IsLetterOrDigit(code[i]))
            {
                if (TryParseWord(code, ref i, out var word, exceptionList))
                {
                    tokenList.Add(word);
                }
            }
            else if (code[i] == '"')
            {
                if (TryParseLiteral(code, ref i, out var literal, exceptionList))
                {
                    tokenList.Add(literal);
                }
            }
            else
            {
                if (TryParseCustom(code, ref i, tokenList.LastOrDefault(), out var result, exceptionList))
                {
                    tokenList.Add(result);
                }
            }
        }
        
        return new TokenizationResult(new TokenSequence(tokenList), exceptionList);
    }

    private static bool TryParseWord(string code, ref int position, out Word word, List<TokenizeException> exceptions)
    {
        word = null;
        var builder = new StringBuilder();
        var start = position;
        for (; position < code.Length; position++)
        {
            if (char.IsLetterOrDigit(code[position]))
            {
                builder.Append(code[position]);
            }
            else
            {
                word = new Word(builder.ToString(), start);
                return true;
            }
        }
        word = new Word(builder.ToString(), start);
        return true;
    }

    private static bool TryParseLiteral(string code, ref int position, out StringLiteral literal, List<TokenizeException> exceptions)
    {
        literal = null;
        var startPosition = position;
        var builder = new StringBuilder();
        position++;
        for (; position < code.Length; position++)
        {
            switch (code[position])
            {
                case EndOfLine:
                    exceptions.Add(new TokenizeException(TokenizerErrorConstants.StringIsNotClosed, startPosition, position));
                    return false;
                case '"':
                    position++;
                    literal = new StringLiteral(builder.ToString(), startPosition);
                    return true;
                case '\\':
                    position++;
                    TryParseEscapedSequence(code, ref position, builder, exceptions);
                    break;
                case '\r':
                    if (code[position..(position + 2)] == EndOfLineCrlf)
                    {
                        exceptions.Add(new TokenizeException(TokenizerErrorConstants.StringIsNotClosed, startPosition, position + 1));
                        position += 2;
                        return false;
                    }

                    builder.Append(code[position]);
                    break;
                default:
                    builder.Append(code[position]);
                    break;
            }
        }

        exceptions.Add(new TokenizeException(TokenizerErrorConstants.StringIsNotClosed, startPosition, position - 1));
        return false;
    }

    private static void TryParseEscapedSequence(string code, ref int position, StringBuilder builder,
        List<TokenizeException> exceptions)
    {
        switch (code[position])
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
                exceptions.Add(new TokenizeException(TokenizerErrorConstants.InvalidEscapeSequence, position - 1, position));
                return;
        }
    }
    
    private static bool TryParseCustom(string code, ref int position, TokenBase lastToken, out TokenBase result, List<TokenizeException> exceptions)
    {
        result = null;
        var startPosition = position;
        if (code.Length > position + 3 && code[position..(position + 4)] == "    "
                                       && (lastToken == null
                                           || lastToken.GetType() == typeof(EndOfLine)
                                           || lastToken.GetType() == typeof(Scope)))
        {
            position += 4;
            result = new Scope(startPosition, 4);
            return true;
        }
        switch (code[position])
        {
            case '[':
                position++;
                result = new OpenSquareBracket(startPosition);
                return true;
            case ']':
                position++;
                result = new CloseSquareBracket(startPosition);
                return true;
            case '(':
                position++;
                result = new OpenParen(startPosition);
                return true;
            case ')':
                position++;
                result = new CloseParen(startPosition);
                return true;
            case ',':
                position++;
                result = new Comma(startPosition);
                return true;
            case '\t':
                position++;
                if (lastToken != null
                    && lastToken.GetType() != typeof(EndOfLine)
                    && lastToken.GetType() != typeof(Scope))
                {
                    result = new WhiteSpace("\t", startPosition);
                    return true;
                }
                result = new Scope(startPosition, 1);
                return true;
            case EndOfLine:
                position++;
                result = new EndOfLine(startPosition, 1);
                return true;
            case ' ':
                position++;
                result = new WhiteSpace(" ", startPosition);
                return true;
            case '\r':
                if (code.Length > position + 1 && code[position..(position + 2)] == EndOfLineCrlf)
                {
                    position += 2;
                    result = new EndOfLine(startPosition, 2);
                    return true;
                }
                position++;
                result = new WhiteSpace("\r", startPosition);
                return true;
            default:
                if (TryParseOperator(code, ref position, out var @operator))
                {
                    result = @operator;
                    return true;
                }
                exceptions.Add(new TokenizeException(TokenizerErrorConstants.UnexpectedToken, position, position));
                position++;
                return false;
        }
    }
    
    private static bool TryParseOperator(string code, ref int position, out Operator @operator)
    {
        var startPosition = position;
        if (code.Length - position >= 2)
        {
            var op = code.Substring(position, 2);
            switch (op)
            {
                case "+=":
                    @operator = new Operator("+=", startPosition);
                    position+=2;
                    return true;
                case "-=":
                    @operator = new Operator("-=", startPosition);
                    position+=2;
                    return true;
                case "++":
                    @operator = new Operator("++", startPosition);
                    position+=2;
                    return true;
                case "--":
                    @operator = new Operator("--", startPosition);
                    position+=2;
                    return true;
                case "*=":
                    @operator = new Operator("*=", startPosition);
                    position+=2;
                    return true;
                case "/=":
                    @operator = new Operator("/=", startPosition);
                    position+=2;
                    return true;
                case "==":
                    @operator = new Operator("==", startPosition);
                    position+=2;
                    return true;
                case "!=":
                    @operator = new Operator("!=", startPosition);
                    position+=2;
                    return true;
                case "<=":
                    @operator = new Operator("<=", startPosition);
                    position+=2;
                    return true;
                case ">=":
                    @operator = new Operator(">=", startPosition);
                    position+=2;
                    return true;
                case "&&":
                    @operator = new Operator("&&", startPosition);
                    position+=2;
                    return true;
                case "||":
                    @operator = new Operator("||", startPosition);
                    position+=2;
                    return true;
                case "%=":
                    @operator = new Operator("%=", startPosition);
                    position+=2;
                    return true;
            }
        }

        switch (code[position])
        {
            case '+':
                @operator = new Operator("+", startPosition);
                break;
            case '-':
                @operator = new Operator("-", startPosition);
                break;
            case '=':
                @operator = new Operator("=", startPosition);
                break;
            case '.':
                @operator = new Operator(".", startPosition);
                break;
            case '/':
                @operator = new Operator("/", startPosition);
                break;
            case '*':
                @operator = new Operator("*", startPosition);
                break;
            case '<':
                @operator = new OpenAngleBracket(startPosition);
                break;
            case '>':
                @operator = new CloseAngleBracket(startPosition);
                break;
            case '!':
                @operator = new Operator("!", startPosition);
                break;
            case '%':
                @operator = new Operator("%", startPosition);
                break;
            default:
                @operator = null;
                return false;
        }

        position++;
        return true;
    }
}