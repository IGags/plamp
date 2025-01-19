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
        if((IteratorVar != null && other.IteratorVar == null) 
           || (IteratorVar == null && other.IteratorVar != null)) return false;
        if((TilCondition != null && other.TilCondition == null) 
           || (TilCondition == null && other.TilCondition != null)) return false;
        if((Counter != null && other.Counter == null) 
           || (Counter == null && other.Counter != null)) return false;
        if((Body != null && other.Body == null) 
           || (Body == null && other.Body != null)) return false;
        return (IteratorVar == null && other.IteratorVar == null 
                || IteratorVar!.Equals(other.IteratorVar))
            && (TilCondition == null && other.TilCondition == null 
                || TilCondition!.Equals(other.TilCondition))
            && (Counter == null && other.Counter == null 
                || Counter!.Equals(other.Counter))
            && (Body == null && other.Body == null 
                || Body!.Equals(other.Body));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), IteratorVar, TilCondition, Counter, Body);
    }
}