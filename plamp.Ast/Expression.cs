using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Parser.Ast;

public abstract record Expression
{
    public abstract System.Linq.Expressions.Expression Compile();

    public abstract Type GetReturnType();
}

public abstract record UnaryExpression(Expression Inner) : Expression;

public abstract record BinaryExpression(Expression Left, Expression Right) : Expression;

public record EmptyExpression() : Expression
{
    public override System.Linq.Expressions.Expression Compile() => System.Linq.Expressions.Expression.Empty();
    public override Type GetReturnType() => typeof(void);
}

public record ConstantExpression(object Value, Type Type) : Expression
{
    public override System.Linq.Expressions.Expression Compile() =>
        System.Linq.Expressions.Expression.Constant(Value, Type);

    public override Type GetReturnType() => Type;
}

public abstract record BaseVariableExpression(VariableDefinition VariableDefinition) : Expression;

public record ParameterExpression(VariableDefinition VariableDefinition) : BaseVariableExpression(VariableDefinition)
{
    private System.Linq.Expressions.ParameterExpression _inner;
    
    public override System.Linq.Expressions.Expression Compile()
    {
        if (_inner != null)
        {
            return _inner;
        }
        _inner = System.Linq.Expressions.Expression.Parameter(VariableDefinition.Type, VariableDefinition.Name);
        return _inner;

    }
    
    public override Type GetReturnType() => VariableDefinition.Type;
}

public record VariableExpression(VariableDefinition VariableDefinition) : BaseVariableExpression(VariableDefinition)
{
    public override System.Linq.Expressions.Expression Compile() => 
        System.Linq.Expressions.Expression.Variable(VariableDefinition.Type, VariableDefinition.Name);

    public override Type GetReturnType() => VariableDefinition.Type;
}

public record CreateVariableExpression(VariableDefinition VariableDefinition) : BaseVariableExpression(VariableDefinition)
{
    private System.Linq.Expressions.ParameterExpression _inner;

    public override System.Linq.Expressions.Expression Compile()
    {
        if (_inner == null)
        {
            _inner = System.Linq.Expressions.Expression.Parameter(VariableDefinition.Type, VariableDefinition.Name);
            return _inner;
        }

        return _inner;
    }
        

    public override Type GetReturnType() => VariableDefinition.Type;
}

public record MemberExpression(string Name) : Expression;

public record CallExpression(Expression From, MemberExpression Member, List<Expression> Args) : Expression
{
}

public record BodyExpression(List<Expression> Expressions, IReadOnlyList<BaseVariableExpression> Variables) : Expression
{
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.Block(
            Variables.Select(x => x.Compile()).Cast<System.Linq.Expressions.ParameterExpression>(), 
            Expressions.Select(x => x.Compile()));
    }

    public override Type GetReturnType() =>
        Expressions.FirstOrDefault(x => x is ReturnExpression)?.GetReturnType() ?? typeof(void);
}

public record FuncExpression(string Name, Type ReturnType, ParameterExpression[] ParameterList, BodyExpression Body) : Expression
{
    public override System.Linq.Expressions.Expression Compile()
    {
        throw new NotImplementedException();
    }

    public override Type GetReturnType() => throw new NotImplementedException();

    public T Compile<T>()
    {
        var @params 
            = ParameterList.Select(x => x.Compile()).Cast<System.Linq.Expressions.ParameterExpression>();
        return System.Linq.Expressions.Expression.Lambda<T>(Body.Compile(), @params).Compile();
    }
}

public record ForExpression(CreateVariableExpression IterableItem, Expression Array, BodyExpression Body) : Expression
{
    //TODO: доделать
    public override System.Linq.Expressions.Expression Compile()
    {
        throw new NotImplementedException();
    }

    public override Type GetReturnType()
    {
        return typeof(void);
    }
}

public record AssignExpression(BaseVariableExpression VariableDefinition, Expression Right) : Expression
{
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.Assign(VariableDefinition.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return typeof(void);
    }
}

public record WhileExpression(Expression Condition, BodyExpression Body) : Expression
{
    //TODO: доделать
    public override System.Linq.Expressions.Expression Compile()
    {
        throw new NotImplementedException();
    }

    public override Type GetReturnType()
    {
        return typeof(void);
    }
}

public record ClauseExpression(Expression Condition, BodyExpression Body) : Expression
{
    public override System.Linq.Expressions.Expression Compile()
    {
        throw new NotImplementedException();
    }

    public override Type GetReturnType()
    {
        return typeof(void);
    }
}

public record ConditionExpression(
    ClauseExpression IfClause,
    List<ClauseExpression> ElifClauseList,
    BodyExpression ElseClause) : Expression
{
    //TODO: компилляция в отдельной сборке
    public override System.Linq.Expressions.Expression Compile()
    {
        if (!ElifClauseList.Any() && ElseClause == null)
        {
            return System.Linq.Expressions.Expression.IfThen(IfClause.Condition.Compile(), IfClause.Body.Compile());
        }
        if (!ElifClauseList.Any())
        {
            return System.Linq.Expressions.Expression.IfThenElse(IfClause.Condition.Compile(), IfClause.Body.Compile(),
                ElseClause.Compile());
        }

        //Чтобы не сломать при нескольких компилляциях
        var clauses = ((IEnumerable<ClauseExpression>)ElifClauseList).Reverse();
        var lastClause = clauses.First();
        System.Linq.Expressions.Expression last;
        if (ElseClause != null)
        {
            last = System.Linq.Expressions.Expression.IfThenElse(lastClause.Condition.Compile(),
                lastClause.Body.Compile(), ElseClause.Compile());
        }
        else
        {
            last = System.Linq.Expressions.Expression.IfThen(lastClause.Condition.Compile(), lastClause.Body.Compile());
        }

        foreach (var clause in clauses.Skip(1))
        {
            last = System.Linq.Expressions.Expression.IfThenElse(clause.Condition.Compile(), clause.Body.Compile(),
                last);
        }
        return System.Linq.Expressions.Expression.IfThenElse(IfClause.Condition.Compile(), IfClause.Body.Compile(), last);
    }

    public override Type GetReturnType()
    {
        return typeof(void);
    }
}

public record AddAndAssignExpression(BaseVariableExpression VariableDefinition, Expression Right) : Expression
{
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.AddAssign(VariableDefinition.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return VariableDefinition.GetReturnType();
    }
}

public record SubAndAssignExpression(BaseVariableExpression VariableDefinition, Expression Right) : Expression
{
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.SubtractAssign(VariableDefinition.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return VariableDefinition.GetReturnType();
    }
}

public record MulAndAssignExpression(BaseVariableExpression VariableDefinition, Expression Right) : Expression
{
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.MultiplyAssign(VariableDefinition.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return VariableDefinition.GetReturnType();
    }
}

public record DivAndAssignExpression(BaseVariableExpression VariableDefinition, Expression Right) : Expression
{
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.DivideAssign(VariableDefinition.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return VariableDefinition.GetReturnType();
    }
}

public record PrefixIncrement(Expression Inner) : UnaryExpression(Inner)
{
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.PreIncrementAssign(Inner.Compile());
    }

    public override Type GetReturnType()
    {
        return Inner.GetReturnType();
    }
}

public record PrefixDecrement(Expression Inner) : UnaryExpression(Inner)
{
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.PreDecrementAssign(Inner.Compile());
    }

    public override Type GetReturnType()
    {
        return Inner.GetReturnType();
    }
}

public record PostfixIncrement(Expression Inner) : UnaryExpression(Inner)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.PostIncrementAssign(Inner.Compile());
    }

    public override Type GetReturnType()
    {
        return Inner.GetReturnType();
    }
}

public record PostfixDecrement(Expression Inner) : UnaryExpression(Inner)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.PostDecrementAssign(Inner.Compile());
    }

    public override Type GetReturnType()
    {
        return Inner.GetReturnType();
    }
}

public record Negate(Expression Inner) : UnaryExpression(Inner)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.Not(Inner.Compile());
    }

    public override Type GetReturnType()
    {
        return typeof(bool);
    }
}

public record UnaryMinus(Expression Inner) : UnaryExpression(Inner)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.Negate(Inner.Compile());
    }

    public override Type GetReturnType()
    {
        return Inner.GetReturnType();
    }
}

public record Equal(Expression Left, Expression Right) : BinaryExpression(Left, Right)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.Equal(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return typeof(bool);
    }
}

public record NotEquals(Expression Left, Expression Right) : BinaryExpression(Left, Right)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.NotEqual(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return typeof(bool);
    }
}

public record LessOrEquals(Expression Left, Expression Right) : BinaryExpression(Left, Right)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.LessThanOrEqual(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return typeof(bool);
    }
}

public record GreaterOrEquals(Expression Left, Expression Right) : BinaryExpression(Left, Right)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.GreaterThanOrEqual(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return typeof(bool);
    }
}

public record Less(Expression Left, Expression Right) : BinaryExpression(Left, Right)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.LessThan(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return typeof(bool);
    }
}

public record Greater(Expression Left, Expression Right) : BinaryExpression(Left, Right)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.GreaterThan(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return typeof(bool);
    }
}

public record And(Expression Left, Expression Right) : BinaryExpression(Left, Right)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.AndAlso(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return typeof(bool);
    }
}

public record Or(Expression Left, Expression Right) : BinaryExpression(Left, Right)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.OrElse(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return typeof(bool);
    }
}

public record Plus(Expression Left, Expression Right) : BinaryExpression(Left, Right)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.Add(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return Left.GetReturnType();
    }
}

public record Minus(Expression Left, Expression Right) : BinaryExpression(Left, Right)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.Subtract(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return Left.GetReturnType();
    }
}

public record Modulo(Expression Left, Expression Right) : BinaryExpression(Left, Right)
{
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.Modulo(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return Left.GetReturnType();
    }
}

public record ModuloAndAssign(BaseVariableExpression Left, Expression Right) : Expression
{
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.ModuloAssign(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return Left.GetReturnType();
    }
}

public record Divide(Expression Left, Expression Right) : BinaryExpression(Left, Right)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.Divide(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return Left.GetReturnType();
    }
}

public record Multiply(Expression Left, Expression Right) : BinaryExpression(Left, Right)
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.Multiply(Left.Compile(), Right.Compile());
    }

    public override Type GetReturnType()
    {
        return Left.GetReturnType();
    }
}

public record ReturnExpression(Expression ReturnValue) : Expression
{
    //TODO: компилляция в отдельном модуле
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.Label(
            System.Linq.Expressions.Expression.Label(ReturnValue.GetReturnType()), ReturnValue.Compile());
    }

    public override Type GetReturnType()
    {
        return ReturnValue.GetReturnType();
    }
}

public record BreakExpression : Expression
{
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.Break(System.Linq.Expressions.Expression.Label());
    }

    public override Type GetReturnType()
    {
        return typeof(void);
    }
}

public record ContinueExpression : Expression
{
    public override System.Linq.Expressions.Expression Compile()
    {
        return System.Linq.Expressions.Expression.Continue(System.Linq.Expressions.Expression.Label());
    }

    public override Type GetReturnType()
    {
        return typeof(void);
    }
}