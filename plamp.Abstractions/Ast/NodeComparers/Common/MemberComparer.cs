using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class MemberComparer : IEqualityComparer<MemberNode>
{
    public bool Equals(MemberNode x, MemberNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.MemberName.Equals(y.MemberName);
    }

    public int GetHashCode(MemberNode obj)
    {
        return HashCode.Combine(obj.MemberName, obj.GetType());
    }
}