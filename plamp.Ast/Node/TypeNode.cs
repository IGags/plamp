using System;
using System.Collections.Generic;
using System.Linq;

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
        if(other == null) return false;
        if (InnerGenerics == null && other.InnerGenerics != null) return false;
        return TypeName == other.TypeName 
               && ((InnerGenerics == null && other.InnerGenerics == null) 
                   || InnerGenerics!.SequenceEqual(other.InnerGenerics!));
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(TypeName);
        if (InnerGenerics == null) return hashCode.ToHashCode();
        foreach (var generic in InnerGenerics)
        {
            hashCode.Add(generic);
        }
        return hashCode.ToHashCode();
    }
}