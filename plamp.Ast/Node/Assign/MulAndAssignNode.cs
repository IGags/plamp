using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public record MulAndAssignNode(NodeBase Variable, NodeBase Right) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Variable;
        yield return Right;
    }

    public virtual bool Equals(MulAndAssignNode other)
    {
        if (other == null) return false;
        return Variable.Equals(other.Variable) && Right.Equals(other.Right);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Variable, Right);
    }
}