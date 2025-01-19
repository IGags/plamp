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
        if((Iterator != null && other.Iterator == null) 
           || (Iterator == null && other.Iterator != null)) return false;
        if((Iterable != null && other.Iterable == null) 
           || (Iterable == null && other.Iterable != null)) return false;
        if((Body != null && other.Body == null) 
           || (Body == null && other.Body != null)) return false;
        return (Iterator == null && other.Iterator == null 
                || Iterator!.Equals(other.Iterator))
               && (Iterator == null && other.Iterator == null 
                   || Iterator!.Equals(other.Iterator))
               && (Body == null && other.Body == null 
                   || Body!.Equals(other.Body));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Iterator, Iterable, Body);
    }
}