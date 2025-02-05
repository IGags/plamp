using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record WhileNode(NodeBase Condition, BodyNode Body) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Condition;
        yield return Body;
    }

    public virtual bool Equals(WhileNode other)
    {
        if (other == null) return false;
        return Condition == other.Condition && Body == other.Body;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Condition, Body);
    }
}