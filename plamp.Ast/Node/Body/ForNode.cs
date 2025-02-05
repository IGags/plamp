using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record ForNode(
    NodeBase IteratorVar, NodeBase TilCondition, NodeBase Counter, BodyNode Body) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return IteratorVar;
        yield return TilCondition;
        yield return Counter;
        yield return Body;
    }

    public virtual bool Equals(ForNode other)
    {
        if(other is null) return false;
        return IteratorVar == other.IteratorVar 
               && TilCondition == other.TilCondition 
               && Counter == other.Counter && Body == other.Body;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IteratorVar, TilCondition, Counter, Body);
    }
}