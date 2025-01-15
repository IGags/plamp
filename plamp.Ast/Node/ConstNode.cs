using System;
using System.Collections.Generic;

namespace plamp.Ast.Node;

public record ConstNode(object Value, Type Type) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }

    public virtual bool Equals(ConstNode other)
    {
        return other != null && Value.Equals(other.Value) && Type == other.Type;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Value, Type);
    }
}