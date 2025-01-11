using System;
using System.Collections.Generic;

namespace plamp.Ast.Node;

public record ConstructorNode(NodeBase Type, List<NodeBase> Args) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        foreach (var argument in Args)
        {
            yield return argument;
        }
    }

    public virtual bool Equals(ConstructorNode other)
    {
        if (other == null 
            || !Type.Equals(other.Type)
            || (Args != null && other.Args == null)
            || (Args == null && other.Args != null)) 
            return false;

        if (other.Args == null && Args == null) return true;
        if (Args!.Count != other.Args!.Count) return false;

        for (var i = 0; i < Args.Count; i++)
        {
            if (!Args[i].Equals(other.Args[i])) return false;
        }
        
        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Type, Args);
    }
}