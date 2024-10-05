using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Enumerations;

public static class OperatorTranslator
{
    public static OperatorEnum ToOperator(this Operator token)
    {
        return token.GetString() switch
        {
            "+" => OperatorEnum.Plus,
            "-" => OperatorEnum.Minus,
            "*" => OperatorEnum.Multiply,
            "/" => OperatorEnum.Divide,
            "=" => OperatorEnum.Assign,
            "==" => OperatorEnum.Equals,
            "!=" => OperatorEnum.NotEquals,
            "<" => OperatorEnum.Lesser,
            ">" => OperatorEnum.Greater,
            "<=" => OperatorEnum.LesserOrEquals,
            ">=" => OperatorEnum.GreaterOrEquals,
            "&&" => OperatorEnum.And,
            "||" => OperatorEnum.Or,
            "!" => OperatorEnum.Not,
            "+=" => OperatorEnum.PlusAndAssign,
            "-=" => OperatorEnum.MinusAndAssign,
            "*=" => OperatorEnum.MultiplyAndAssign,
            "/=" => OperatorEnum.DivideAndAssign,
            "++" => OperatorEnum.Increment,
            "--" => OperatorEnum.Decrement,
            "." => OperatorEnum.Call,
            "%" => OperatorEnum.Modulo,
            "%=" => OperatorEnum.ModuloAndAssign,
            _ => OperatorEnum.None
        };
    }

    public static int GetPrecedence(this OperatorEnum op, bool isNud)
    {
        if (isNud)
        {
            return op switch
            {
                OperatorEnum.Minus => 100,
                OperatorEnum.Not => 99,
                OperatorEnum.Increment => 98,
                OperatorEnum.Decrement => 97,
                _ => int.MinValue
            };
        }

        return op switch
        {
            OperatorEnum.Multiply => 50,
            OperatorEnum.Divide => 49,
            OperatorEnum.Plus => 48,
            OperatorEnum.Minus => 47,
            OperatorEnum.Lesser => 46,
            OperatorEnum.Modulo => 45,
            OperatorEnum.Greater => 44,
            OperatorEnum.LesserOrEquals => 43,
            OperatorEnum.GreaterOrEquals => 42,
            OperatorEnum.Equals => 41,
            OperatorEnum.NotEquals => 40,
            OperatorEnum.And => 39,
            OperatorEnum.Or => 38,
            OperatorEnum.Assign => 20,
            OperatorEnum.PlusAndAssign => 19,
            OperatorEnum.MinusAndAssign => 18,
            OperatorEnum.MultiplyAndAssign => 17,
            OperatorEnum.DivideAndAssign => 16,
            OperatorEnum.ModuloAndAssign => 15,
            _ => int.MinValue
        };
    }
}