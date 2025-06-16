using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.ControlFlow;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class ReturnComparer : IEqualityComparer<ReturnNode>
{
    public bool Equals(ReturnNode x, ReturnNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(ReturnNode obj)
    {
        return obj.ReturnValue != null ? obj.ReturnValue.GetHashCode() : 0;
    }
}