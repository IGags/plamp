using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class WhileComparer : IEqualityComparer<WhileNode>
{
    public bool Equals(WhileNode? x, WhileNode? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(WhileNode obj)
    {
        return HashCode.Combine(obj.Condition, obj.Body);
    }
}