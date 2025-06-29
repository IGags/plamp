using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class UseComparer : IEqualityComparer<UseNode>
{
    public bool Equals(UseNode x, UseNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(UseNode obj)
    {
        return obj.Namespace != null ? obj.Namespace.GetHashCode() : 0;
    }
}