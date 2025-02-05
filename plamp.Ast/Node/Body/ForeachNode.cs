using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record ForeachNode(NodeBase Iterator, NodeBase Iterable, BodyNode Body) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Iterator;
        yield return Iterable;
        yield return Body;
    }

    public virtual bool Equals(ForeachNode other)
    {
        if(other is null) return false;
        return Iterator == other.Iterator && Iterable == other.Iterable && Body == other.Body;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Iterator, Iterable, Body);
    }
}