using plamp.Native.Tokenization.Token;

namespace plamp.Native.Tokenization.Enumerations;

public static class OperatorTranslator
{
    public static OperatorEnum ToOperator(this string token)
    {
        return token switch
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
            "." => OperatorEnum.MemberAccess,
            "%" => OperatorEnum.Modulo,
            "%=" => OperatorEnum.ModuloAndAssign,
            "|" => OperatorEnum.BitwiseOr,
            "&" => OperatorEnum.BitwiseAnd,
            "^" => OperatorEnum.Xor,
            "&=" => OperatorEnum.AndAndAssign,
            "|=" => OperatorEnum.OrAndAssign,
            "^=" => OperatorEnum.XorAndAssign,
            _ => OperatorEnum.None
        };
    }

    public static int GetPrecedence(this OperatorToken op, bool isNud)
    {
        if (isNud)
        {
            return op.Operator switch
            {
                OperatorEnum.Minus => 100,
                OperatorEnum.Not => 99,
                OperatorEnum.Increment => 98,
                OperatorEnum.Decrement => 97,
                _ => int.MinValue
            };
        }

        return op.Operator switch
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
            OperatorEnum.BitwiseAnd => 39,
            OperatorEnum.Xor => 38,
            OperatorEnum.BitwiseOr => 37,
            OperatorEnum.And => 36,
            OperatorEnum.Or => 35,
            OperatorEnum.Assign => 20,
            OperatorEnum.PlusAndAssign => 19,
            OperatorEnum.MinusAndAssign => 18,
            OperatorEnum.MultiplyAndAssign => 17,
            OperatorEnum.DivideAndAssign => 16,
            OperatorEnum.ModuloAndAssign => 15,
            OperatorEnum.AndAndAssign => 14,
            OperatorEnum.OrAndAssign => 13,
            OperatorEnum.XorAndAssign => 12,
            _ => int.MinValue
        };
    }
}