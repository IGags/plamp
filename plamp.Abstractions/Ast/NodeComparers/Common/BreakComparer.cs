using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.ControlFlow;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class BreakComparer : IEqualityComparer<BreakNode>
{
    public bool Equals(BreakNode? x, BreakNode? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null) return false;
        if (y == null) return false;
        return true;
    }

    public int GetHashCode(BreakNode obj)
    {
        return 0;
    }
}