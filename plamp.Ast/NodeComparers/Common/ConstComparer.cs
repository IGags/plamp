using System;
using System.Collections.Generic;
using plamp.Ast.Node;

namespace plamp.Ast.NodeComparers.Common;

public class ConstComparer : IEqualityComparer<ConstNode>
{
    public bool Equals(ConstNode x, ConstNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(ConstNode obj)
    {
        return HashCode.Combine(obj.Value, obj.Type);
    }
}