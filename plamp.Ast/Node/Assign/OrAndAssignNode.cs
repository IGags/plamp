using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public record OrAndAssignNode(NodeBase VariableDefinition, NodeBase Right) : BaseAssignNode(Right)
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return VariableDefinition;
        yield return Right;
    }

    public virtual bool Equals(OrAndAssignNode other)
    {
        if (other == null) return false;
        return VariableDefinition.Equals(other.VariableDefinition) && Right.Equals(other.Right);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(VariableDefinition, Right);
    }
}