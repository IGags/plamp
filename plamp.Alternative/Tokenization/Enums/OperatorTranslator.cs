using plamp.Alternative.Tokenization.Token;

namespace plamp.Alternative.Tokenization.Enums;

public static class OperatorTranslator
{
    public static OperatorEnum ToOperator(this string token)
    {
        return token switch
        {
            "+" => OperatorEnum.Add,
            "-" => OperatorEnum.Sub,
            "*" => OperatorEnum.Mul,
            "/" => OperatorEnum.Div,
            ":=" => OperatorEnum.Assign,
            "=" => OperatorEnum.Equals,
            "!=" => OperatorEnum.NotEquals,
            "<" => OperatorEnum.Lesser,
            ">" => OperatorEnum.Greater,
            "<=" => OperatorEnum.LesserOrEquals,
            ">=" => OperatorEnum.GreaterOrEquals,
            "&&" => OperatorEnum.And,
            "||" => OperatorEnum.Or,
            "!" => OperatorEnum.Not,
            "++" => OperatorEnum.Increment,
            "--" => OperatorEnum.Decrement,
            "%" => OperatorEnum.Modulo,
            "." => OperatorEnum.Access,
            _ => OperatorEnum.None
        };
    }

    public static int GetPrecedence(this OperatorToken op, bool isNud)
    {
        if (isNud)
        {
            return op.Operator switch
            {
                OperatorEnum.Sub => 100,
                OperatorEnum.Not => 99,
                OperatorEnum.Increment => 98,
                OperatorEnum.Decrement => 97,
                _ => int.MinValue
            };
        }

        return op.Operator switch
        {
            OperatorEnum.Mul => 50,
            OperatorEnum.Div => 49,
            OperatorEnum.Add => 48,
            OperatorEnum.Sub => 47,
            OperatorEnum.Lesser => 46,
            OperatorEnum.Modulo => 45,
            OperatorEnum.Greater => 44,
            OperatorEnum.LesserOrEquals => 43,
            OperatorEnum.GreaterOrEquals => 42,
            OperatorEnum.Equals => 41,
            OperatorEnum.NotEquals => 40,
            OperatorEnum.And => 36,
            OperatorEnum.Or => 35,
            OperatorEnum.Assign => 20,
            _ => int.MinValue
        };
    }
}