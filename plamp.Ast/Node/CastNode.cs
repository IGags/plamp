using System;
using System.Collections.Generic;

namespace plamp.Ast.Node;

public record CastNode(NodeBase ToType, NodeBase Inner) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return ToType;
        yield return Inner;
    }

    public virtual bool Equals(CastNode other)
    {
        if (other == null) return false;
        return ToType == other.ToType && Inner == other.Inner;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), ToType, Inner);
    }
}