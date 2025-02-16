using System;
using System.Collections.Generic;
using plamp.Ast.Node.Assign;

namespace plamp.Ast.NodeComparers.Common;

public class XorAndAssignComparer : IEqualityComparer<XorAndAssignNode>
{
    public bool Equals(XorAndAssignNode x, XorAndAssignNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(XorAndAssignNode obj)
    {
        return HashCode.Combine(obj.Right, obj.VariableDefinition);
    }
}