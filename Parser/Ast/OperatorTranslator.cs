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
            _ => Operator.None
        };
    }

    public static int GetPrecedence(this Operator op)
    {
        return op switch
        {

        };
    }
}