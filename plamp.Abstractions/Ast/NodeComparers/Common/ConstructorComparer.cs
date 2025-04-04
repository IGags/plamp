using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class ConstructorComparer : IEqualityComparer<ConstructorNode>
{
    public bool Equals(ConstructorNode x, ConstructorNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(ConstructorNode obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.Type);
        foreach (var arg in obj.Args)
        {
            hashCode.Add(arg);
        }
        return hashCode.ToHashCode();
    }
}