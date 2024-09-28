using System.Collections.Generic;
using System.Linq;
using System.Text;
using Parser.Token;

namespace Parser;

public static class MplgTokenizer
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
                tokenList.Add(ParseCustom(code, ref i));
            }
        }

        if (tokenList.Last().GetType() != typeof(EOF))
        {
            tokenList.Add(new EOF());
        }
        
        return new TokenSequence(tokenList);
    }

    private static Word ParseWord(string code, ref int position)
    {
        var builder = new StringBuilder();
        for (; position < code.Length; position++)
        {
            if (char.IsLetterOrDigit(code[position]))
            {
                builder.Append(code[position]);
            }
        }
        return new Word(builder.ToString());
        void Throw(int pos) => throw new TokenizeException($"Invalid generic type definition: {builder}", pos);
    }

    //TODO: escape sequences
    private static StringLiteral ParseLiteral(string code, ref int position)
    {
        var builder = new StringBuilder();
        position++;
        for (; position < code.Length; position++)
        {
            if (code[position] == '"')
            {
                position++;
                return new StringLiteral(builder.ToString());
            }

            builder.Append(code[position]);
        }

        throw new TokenizeException("Строка не закрыта", position);
    }

    private static TokenBase ParseCustom(string code, ref int position)
    {
        switch (code[position])
        {
            case '[':
                position++;
                return new OpenSquareBracket();
            case ']':
                position++;
                return new CloseSquareBracket();
            case '(':
                position++;
                return new OpenBracket();
            case ')':
                position++;
                return new CloseBracket();
            case ',':
                position++;
                return new Comma();
            case '\t':
                position++;
                return new Scope();
            case '\n':
                position++;
                return new EOF();
            case ' ':
                position++;
                return new WhiteSpace();
            case '\r':
                position++;
                //TODO: доопределить пробел
                return new WhiteSpace();
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
        if (code.Length - position >= 2)
        {
            var op = code.Substring(position, 2);
            switch (op)
            {
                case "+=":
                    @operator = new Operator("+=");
                    position+=2;
                    return true;
                case "-=":
                    @operator = new Operator("-=");
                    position+=2;
                    return true;
                case "++":
                    @operator = new Operator("++");
                    position+=2;
                    return true;
                case "--":
                    @operator = new Operator("--");
                    position+=2;
                    return true;
                case "*=":
                    @operator = new Operator("*=");
                    position+=2;
                    return true;
                case "/=":
                    @operator = new Operator("/=");
                    position+=2;
                    return true;
                case "==":
                    @operator = new Operator("==");
                    position+=2;
                    return true;
                case "!=":
                    @operator = new Operator("!=");
                    position+=2;
                    return true;
                case "<=":
                    @operator = new Operator("<=");
                    position+=2;
                    return true;
                case ">=":
                    @operator = new Operator(">=");
                    position+=2;
                    return true;
                case "&&":
                    @operator = new Operator("&&");
                    position+=2;
                    return true;
                case "||":
                    @operator = new Operator("||");
                    position+=2;
                    return true;
            }
        }

        switch (code[position])
        {
            case '+':
                @operator = new Operator("+");
                break;
            case '-':
                @operator = new Operator("-");
                break;
            case '=':
                @operator = new Operator("=");
                break;
            case '.':
                @operator = new Operator(".");
                break;
            case '/':
                @operator = new Operator("/");
                break;
            case '^':
                @operator = new Operator("^");
                break;
            case '*':
                @operator = new Operator("*");
                break;
            case '<':
                @operator = new Operator("<");
                break;
            case '>':
                @operator = new Operator(">");
                break;
            case '!':
                @operator = new Operator("!");
                break;
            default:
                @operator = null;
                return false;
        }

        position++;
        return true;
    }
}