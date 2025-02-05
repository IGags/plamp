using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public record AddAndAssignNode(NodeBase Member, NodeBase Right) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Member;
        yield return Right;
    }

    public virtual bool Equals(AddAndAssignNode other)
    {
        if (other == null) return false;
        return Member.Equals(other.Member) && Right.Equals(other.Right);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Member, Right);
    }
}