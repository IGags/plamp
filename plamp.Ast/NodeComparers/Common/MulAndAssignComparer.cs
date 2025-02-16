using System;
using System.Collections.Generic;
using plamp.Ast.Node.Assign;

namespace plamp.Ast.NodeComparers.Common;

public class MulAndAssignComparer : IEqualityComparer<MulAndAssignNode>
{
    public bool Equals(MulAndAssignNode x, MulAndAssignNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(MulAndAssignNode obj)
    {
        return HashCode.Combine(obj.Variable, obj.Right);
    }
}