using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public record SubAndAssignNode(NodeBase Variable, NodeBase Right) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Variable;
        yield return Right;
    }

    public virtual bool Equals(SubAndAssignNode other)
    {
        if (other == null) return false;
        return Variable.Equals(other.Variable) && Right.Equals(other.Right);
    }
}