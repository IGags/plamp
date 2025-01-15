using System.Collections.Generic;

namespace plamp.Ast.Node;

public record IndexerNode(NodeBase ToIndex, List<NodeBase> Arguments) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return ToIndex;
        foreach (var argument in Arguments)
        {
            yield return argument;
        }
    }

    public virtual bool Equals(IndexerNode other)
    {
        if(other == null || !ToIndex.Equals(other.ToIndex)) return false;
        if(other.Arguments == null && Arguments == null) return true;
        if((other.Arguments != null && Arguments == null) 
           || (other.Arguments == null && Arguments != null)
           || other.Arguments!.Count != Arguments!.Count) return false;
        for (var i = 0; i < Arguments.Count; i++)
        {
            if(Arguments[i] == null && other.Arguments[i] == null) continue;
            if(!Arguments[i].Equals(other.Arguments[i])) return false;
        }

        return true;
    }
}