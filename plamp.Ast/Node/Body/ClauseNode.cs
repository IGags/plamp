using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record ClauseNode(NodeBase Predicate, BodyNode Body) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Predicate;
        yield return Body;
    }

    public virtual bool Equals(ClauseNode other)
    {
        if (other == null) return false;
        return Predicate == other.Predicate && Body == other.Body;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Predicate, Body);
    }
}