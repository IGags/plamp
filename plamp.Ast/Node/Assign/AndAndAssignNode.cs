using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public record AndAndAssignNode(NodeBase DefinitionNode, NodeBase Right) : BaseAssignNode(Right)
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return DefinitionNode;
        yield return Right;
    }
    
    public virtual bool Equals(AndAndAssignNode other)
    {
        if (other == null) return false;
        return DefinitionNode.Equals(other.DefinitionNode) && Right.Equals(other.Right);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DefinitionNode, Right);
    }
}