using System.Collections.Generic;
using plamp.Ast.Node.Unary;

namespace plamp.Ast.NodeComparers.Common;

public class UnaryComparer : IEqualityComparer<UnaryBaseNode>
{
    public bool Equals(UnaryBaseNode x, UnaryBaseNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(UnaryBaseNode obj)
    {
        return obj.Inner.GetHashCode() ^ GetType().GetHashCode();
    }
}