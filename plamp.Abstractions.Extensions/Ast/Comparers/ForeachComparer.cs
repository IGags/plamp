using System;
using System.Collections.Generic;
using plamp.Abstractions.Extensions.Ast.Node;

namespace plamp.Abstractions.Extensions.Ast.Comparers;

public class ForeachComparer : IEqualityComparer<ForeachNode>
{
    public bool Equals(ForeachNode x, ForeachNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(ForeachNode obj)
    {
        return HashCode.Combine(obj.Iterator, obj.Iterable, obj.Body);
    }
}