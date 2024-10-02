using System.Collections.Generic;
using System.Linq;
using System.Text;
using plamp.Native.Token;

namespace plamp.Native;

public static class PlampNativeTokenizer
{
    public static TokenSequence Tokenize(this string code)
    {
        var tokenList = new List<TokenBase>();
        for(var i = 0; i < code.Length;)
        {
            if (char.IsLetterOrDigit(code[i]))
            {
                tokenList.Add(ParseWord(code, ref i));
            }
            else if (code[i] == '"')
            {
                tokenList.Add(ParseLiteral(code, ref i));
            }
            else
            {
                tokenList.Add(ParseCustom(code, ref i, tokenList.LastOrDefault()));
            }
        }

        if (tokenList.Last().GetType() != typeof(EOF))
        {
            tokenList.Add(new EOF(code.Length));
        }
        
        return new TokenSequence(tokenList);
    }

    private static Word ParseWord(string code, ref int position)
    {
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
                return new Word(builder.ToString(), start);
            }
        }
        return new Word(builder.ToString(), start);
    }

    //TODO: escape sequences
    private static StringLiteral ParseLiteral(string code, ref int position)
    {
        var startPosition = position;
        var builder = new StringBuilder();
        position++;
        for (; position < code.Length; position++)
        {
            if (code[position] == '"')
            {
                position++;
                return new StringLiteral(builder.ToString(), startPosition);
            }

            builder.Append(code[position]);
        }

        throw new TokenizeException("String is not closed", position);
    }

    private static TokenBase ParseCustom(string code, ref int position, TokenBase lastToken)
    {
        var startPosition = position;
        if (code.Length > position + 4 && code[position..(position + 4)] == "    "
                                       && (lastToken == null
                                           || lastToken.GetType() == typeof(EOF)
                                           || lastToken.GetType() == typeof(Scope)))
        {
            position += 4;
            return new Scope(startPosition, 4);
        }
        switch (code[position])
        {
            case '[':
                position++;
                return new OpenSquareBracket(startPosition);
            case ']':
                position++;
                return new CloseSquareBracket(startPosition);
            case '(':
                position++;
                return new OpenBracket(startPosition);
            case ')':
                position++;
                return new CloseBracket(startPosition);
            case ',':
                position++;
                return new Comma(startPosition);
            case '\t':
                position++;
                return new Scope(startPosition, 1);
            case '\n':
                position++;
                return new EOF(startPosition);
            case ' ':
                position++;
                return new WhiteSpace(startPosition);
            case '\r':
                position++;
                //TODO: доопределить пробел
                return new WhiteSpace(startPosition);
            default:
                if (TryParseOperator(code, ref position, out var @operator))
                {
                    return @operator;
                }

                throw new TokenizeException("Unexpected token", position);
        }
    }

    //TODO: операторы надо подключать плагинами
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