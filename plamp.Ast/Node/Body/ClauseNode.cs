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
        if(Predicate == null && other.Predicate != null) return false;
        if(Predicate != null && other.Predicate == null) return false;
        if(Predicate != null && !Predicate.Equals(other.Predicate)) return false;
        return Body == other.Body;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Predicate, Body);
    }
}