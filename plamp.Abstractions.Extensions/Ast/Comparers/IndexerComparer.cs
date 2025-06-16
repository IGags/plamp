using System;
using System.Collections.Generic;
using plamp.Abstractions.Extensions.Ast.Node;

namespace plamp.Abstractions.Extensions.Ast.Comparers;

public class IndexerComparer : IEqualityComparer<IndexerNode>
{
    public bool Equals(IndexerNode x, IndexerNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(IndexerNode obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.ToIndex);
        foreach (var argument in obj.Arguments)
        {
            hashCode.Add(argument);
        }
        return hashCode.ToHashCode();
    }
}