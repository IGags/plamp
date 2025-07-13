using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Unary;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class UnaryComparer : IEqualityComparer<BaseUnaryNode>
{
    public bool Equals(BaseUnaryNode? x, BaseUnaryNode? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(BaseUnaryNode obj)
    {
        return obj.Inner.GetHashCode() ^ GetType().GetHashCode();
    }
}