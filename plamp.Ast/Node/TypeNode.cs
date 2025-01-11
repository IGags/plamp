using System;
using System.Collections.Generic;

namespace plamp.Ast.Node;

public record TypeNode(NodeBase TypeName, List<NodeBase> InnerGenerics) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return TypeName;
        foreach (var generic in InnerGenerics)
        {
            yield return generic;
        }
    }

    public virtual bool Equals(TypeNode other)
    {
        if (other == null
            || !other.TypeName.Equals(TypeName)) return false;

        if (InnerGenerics == null && other.InnerGenerics == null) return true;

        if (InnerGenerics == null 
            || other.InnerGenerics == null
            || InnerGenerics.Count != other.InnerGenerics.Count) return false;
        
        for (var i = 0; i < InnerGenerics.Count; i++)
        {
            if(InnerGenerics[i] == null && other.InnerGenerics[i] == null) continue;
            if(!InnerGenerics[i].Equals(other.InnerGenerics[i])) return false;
        }
        
        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), TypeName, InnerGenerics);
    }
}