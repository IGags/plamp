using System.Collections.Generic;

namespace Parser.Ast;

public abstract record Expression
{
    public abstract System.Linq.Expressions.Expression Compile();
}

public abstract record UnaryExpression(Expression Inner) : Expression;

public abstract record BinaryExpression(Expression Left, Expression Right) : Expression;

public record ValueExpression(string Value) : Expression;

public record ConstructorExpression(TypeDescription Type) : Expression;

public record VariableExpression(VariableDefinition VariableDefinition) : Expression;

public record CreateVariableExpression(VariableDefinition VariableDefinition) : VariableExpression(VariableDefinition);

public record CallExpression(FunctionDefinition Function, List<Expression> Arguments) : Expression
{
}

public record ForExpression(VariableDefinition IterableItem, Expression Array, BodyExpression Body) : Expression
{
}

public record AssignExpression(VariableExpression VariableDefinition, Expression Right) : Expression;

public record WhileExpression(Expression Condition, BodyExpression Body) : Expression
{
}

public record ClauseExpression(Expression Condition, BodyExpression Body) : Expression
{
}

public record ConditionExpression(
    ClauseExpression IfClause,
    List<ClauseExpression> ElifClauseList,
    BodyExpression ElseClause) : Expression
{
}

public record AddAndAssignExpression(VariableDefinition VariableDefinition, Expression Right) : Expression;

public record SubAndAssignExpression(VariableDefinition VariableDefinition, Expression Right) : Expression;

public record MulAndAssignExpression(VariableDefinition VariableDefinition, Expression Right) : Expression;

public record DivAndAssignExpression(VariableDefinition VariableDefinition, Expression Right) : Expression;

public record PowAndAssignExpression(VariableDefinition VariableDefinition, Expression Right) : Expression;

public record Increment(Expression Inner) : UnaryExpression(Inner);

public record Decrement(Expression Inner) : UnaryExpression(Inner);

public record Negate(Expression Inner) : UnaryExpression(Inner);

public record UnaryMinus(Expression Inner) : UnaryExpression(Inner);

public record Equals(Expression Left, Expression Right) : BinaryExpression(Left, Right);

public record NotEquals(Expression Left, Expression Right) : BinaryExpression(Left, Right);

public record LessOrEquals(Expression Left, Expression Right) : BinaryExpression(Left, Right);

public record GreaterOrEquals(Expression Left, Expression Right) : BinaryExpression(Left, Right);

public record Less(Expression Left, Expression Right) : BinaryExpression(Left, Right);

public record Greater(Expression Left, Expression Right) : BinaryExpression(Left, Right);

public record And(Expression Left, Expression Right) : BinaryExpression(Left, Right);

public record Or(Expression Left, Expression Right) : BinaryExpression(Left, Right);

public record Plus(Expression Left, Expression Right) : BinaryExpression(Left, Right);

public record Minus(Expression Left, Expression Right) : BinaryExpression(Left, Right);

public record Divide(Expression Left, Expression Right) : BinaryExpression(Left, Right);

public record Multiply(Expression Left, Expression Right) : BinaryExpression(Left, Right);

public record ReturnExpression(Expression ReturnValue) : Expression;

public record BreakExpression : Expression;

public record ContinueExpression : Expression;