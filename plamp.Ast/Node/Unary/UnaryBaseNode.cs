using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Unary;

public abstract record UnaryBaseNode(NodeBase Inner) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Inner;
    }

    public virtual bool Equals(UnaryBaseNode other)
    {
        if (other == null) return false;
        return Inner == other.Inner;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Inner, base.GetHashCode());
    }
}