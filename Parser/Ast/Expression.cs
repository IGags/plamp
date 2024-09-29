using System;
using System.Collections.Generic;
using System.Reflection;

namespace Parser.Ast;

public abstract record Expression
{
    public abstract System.Linq.Expressions.Expression Compile();

    public abstract Type GetReturnType();
}

public abstract record UnaryExpression(Expression Inner) : Expression;

public abstract record BinaryExpression(Expression Left, Expression Right) : Expression;

public record EmptyExpression() : Expression;

public record ConstantExpression(object Value, Type Type) : Expression;

public abstract record BaseVariableExpression(VariableDefinition VariableDefinition) : Expression;

public record VariableExpression(VariableDefinition VariableDefinition) : BaseVariableExpression(VariableDefinition);

public record CreateVariableExpression(VariableDefinition VariableDefinition) : BaseVariableExpression(VariableDefinition);

public record CallExpression(MethodInfo Method, List<Expression> Arguments) : Expression
{
}

public record ForExpression(CreateVariableExpression IterableItem, Expression Array, BodyExpression Body) : Expression
{
}

public record AssignExpression(BaseVariableExpression VariableDefinition, Expression Right) : Expression;

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

public record AddAndAssignExpression(VariableExpression VariableDefinition, Expression Right) : Expression;

public record SubAndAssignExpression(VariableExpression VariableDefinition, Expression Right) : Expression;

public record MulAndAssignExpression(VariableExpression VariableDefinition, Expression Right) : Expression;

public record DivAndAssignExpression(VariableExpression VariableDefinition, Expression Right) : Expression;

public record PrefixIncrement(Expression Inner) : UnaryExpression(Inner);

public record PrefixDecrement(Expression Inner) : UnaryExpression(Inner);

public record PostfixIncrement(Expression Inner) : UnaryExpression(Inner);

public record PostfixDecrement(Expression Inner) : UnaryExpression(Inner);

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