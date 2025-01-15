using System;
using System.Collections.Generic;

namespace plamp.Ast.Node;

public record CallNode(NodeBase From, List<NodeBase> Args) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return From;
        foreach (var arg in Args)
        {
            yield return arg;
        }
    }

    public virtual bool Equals(CallNode other)
    {
        if(other == null || !From.Equals(other.From)) return false;
        if(other.Args == null && Args == null) return true;
        if((other.Args != null && Args == null) 
           || (other.Args == null && Args != null)
           || other.Args!.Count != Args!.Count) return false;
        for (var i = 0; i < Args.Count; i++)
        {
            if(Args[i] == null && other.Args[i] == null) continue;
            if(!Args[i].Equals(other.Args[i])) return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), From, Args);
    }
}