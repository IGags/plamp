using System;
using System.Collections.Generic;
using plamp.Ast.Node.Binary;

namespace plamp.Ast.NodeComparers.Common;

public class BinaryComparer : IEqualityComparer<BaseBinaryNode>
{
    public bool Equals(BaseBinaryNode x, BaseBinaryNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(BaseBinaryNode obj)
    {
        return HashCode.Combine(obj.Left, obj.Right);
    }
}