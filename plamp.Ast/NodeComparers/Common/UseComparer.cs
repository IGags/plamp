using System.Collections.Generic;
using plamp.Ast.Node;

namespace plamp.Ast.NodeComparers.Common;

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
        return obj.Assembly != null ? obj.Assembly.GetHashCode() : 0;
    }
}