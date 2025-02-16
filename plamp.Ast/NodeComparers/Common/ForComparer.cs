using System;
using System.Collections.Generic;
using plamp.Ast.Node.Body;

namespace plamp.Ast.NodeComparers.Common;

public class ForComparer : IEqualityComparer<ForNode>
{
    public bool Equals(ForNode x, ForNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(ForNode obj)
    {
        return HashCode.Combine(obj.IteratorVar, obj.TilCondition, obj.Counter, obj.Body);
    }
}