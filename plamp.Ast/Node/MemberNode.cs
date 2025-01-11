using System;
using System.Collections.Generic;

namespace plamp.Ast.Node;

public record MemberNode(string MemberName) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }

    public virtual bool Equals(MemberNode other)
    {
        return other != null && MemberName.Equals(other.MemberName);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), MemberName);
    }
}