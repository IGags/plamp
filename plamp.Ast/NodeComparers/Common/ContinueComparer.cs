using System.Collections.Generic;
using plamp.Ast.Node.ControlFlow;

namespace plamp.Ast.NodeComparers.Common;

public class ContinueComparer : IEqualityComparer<ContinueNode>
{
    public bool Equals(ContinueNode x, ContinueNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null) return false;
        if (y == null) return false;
        return true;
    }

    public int GetHashCode(ContinueNode obj)
    {
        return 0;
    }
}