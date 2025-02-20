using System;
using System.Collections.Generic;
using plamp.Ast.Node.Assign;

namespace plamp.Ast.NodeComparers.Common;

public class AndAndAssignComparer : IEqualityComparer<AndAndAssignNode>
{
    public bool Equals(AndAndAssignNode x, AndAndAssignNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(AndAndAssignNode obj)
    {
        return HashCode.Combine(obj.Member, obj.Right);
    }
}