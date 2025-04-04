using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class ClauseComparer : IEqualityComparer<ClauseNode>
{
    public bool Equals(ClauseNode x, ClauseNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(ClauseNode obj)
    {
        return HashCode.Combine(obj.Predicate, obj.Body);
    }
}