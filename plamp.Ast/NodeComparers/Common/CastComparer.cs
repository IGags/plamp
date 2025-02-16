using System;
using System.Collections.Generic;
using plamp.Ast.Node;

namespace plamp.Ast.NodeComparers.Common;

public class CastComparer : IEqualityComparer<CastNode>
{
    public bool Equals(CastNode x, CastNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(CastNode obj)
    {
        return HashCode.Combine(obj.ToType, obj.Inner);
    }
}