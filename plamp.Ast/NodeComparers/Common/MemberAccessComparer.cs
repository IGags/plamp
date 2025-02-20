using System;
using System.Collections.Generic;
using plamp.Ast.Node;

namespace plamp.Ast.NodeComparers.Common;

public class MemberAccessComparer : IEqualityComparer<MemberAccessNode>
{
    public bool Equals(MemberAccessNode x, MemberAccessNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(MemberAccessNode obj)
    {
        return HashCode.Combine(obj.From, obj.Member);
    }
}