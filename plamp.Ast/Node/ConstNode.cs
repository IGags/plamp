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
        if(other == null) return false;
        if(Value == null && other.Value == null && Type == other.Type) return true;
        if (Value == null || Type == null) return false;
        return Value.Equals(other.Value) && Type == other.Type;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Value, Type);
    }
}