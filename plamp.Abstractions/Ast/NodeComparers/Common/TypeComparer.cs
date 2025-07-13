using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class TypeComparer : IEqualityComparer<TypeNode>
{
    public bool Equals(TypeNode? x, TypeNode? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(TypeNode obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.TypeName);
        foreach (var generic in obj.InnerGenerics)
        {
            hashCode.Add(generic);
        }
        return hashCode.ToHashCode();
    }
}