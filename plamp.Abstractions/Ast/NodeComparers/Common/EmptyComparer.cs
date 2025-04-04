using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class EmptyComparer : IEqualityComparer<EmptyNode>
{
    public bool Equals(EmptyNode x, EmptyNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(EmptyNode obj)
    {
        return 0;
    }
}