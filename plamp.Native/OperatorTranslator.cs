namespace Parser.Ast;

public static class OperatorTranslator
{
    public static Operator ToOperator(this Token.Operator token)
    {
        return token.GetString() switch
        {
            "+" => Operator.Plus,
            "-" => Operator.Minus,
            "*" => Operator.Multiply,
            "/" => Operator.Divide,
            "=" => Operator.Assign,
            "==" => Operator.Equals,
            "!=" => Operator.NotEquals,
            "<" => Operator.Lesser,
            ">" => Operator.Greater,
            "<=" => Operator.LesserOrEquals,
            ">=" => Operator.GreaterOrEquals,
            "&&" => Operator.And,
            "||" => Operator.Or,
            "!" => Operator.Not,
            "+=" => Operator.PlusAndAssign,
            "-=" => Operator.MinusAndAssign,
            "*=" => Operator.MultiplyAndAssign,
            "/=" => Operator.DivideAndAssign,
            "++" => Operator.Increment,
            "--" => Operator.Decrement,
            "." => Operator.Call,
            "%" => Operator.Modulo,
            "%=" => Operator.ModuloAndAssign,
            _ => Operator.None
        };
    }

    public static int GetPrecedence(this Operator op, bool isNud)
    {
        if (isNud)
        {
            return op switch
            {
                Operator.Minus => 100,
                Operator.Not => 99,
                Operator.Increment => 98,
                Operator.Decrement => 97,
                _ => throw new ParserException($"Invalid operator {op} in current context")
            };
        }

        return op switch
        {
            Operator.Call => 54,
            Operator.Multiply => 50,
            Operator.Divide => 49,
            Operator.Plus => 48,
            Operator.Minus => 47,
            Operator.Lesser => 46,
            Operator.Modulo => 45,
            Operator.Greater => 44,
            Operator.LesserOrEquals => 43,
            Operator.GreaterOrEquals => 42,
            Operator.Equals => 41,
            Operator.NotEquals => 40,
            Operator.And => 39,
            Operator.Or => 38,
            _ => throw new ParserException($"Invalid operator {op} in currnet context")
        };
    }
}