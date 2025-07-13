using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class LiteralComparer : IEqualityComparer<LiteralNode>
{
    public bool Equals(LiteralNode? x, LiteralNode? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(LiteralNode obj)
    {
        return HashCode.Combine(obj.Value, obj.Type);
    }
}