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
        if((Condition == null && other.Condition != null)
           || (Condition != null && other.Condition == null)
           || (Body == null && other.Body != null)
           || (Body != null && other.Body == null)) return false;
        return (Body == null || Body.Equals(other.Body)) 
               && (Condition == null || Condition.Equals(other.Condition));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Condition, Body);
    }
}