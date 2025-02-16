using System;
using System.Collections;
using System.Collections.Generic;
using plamp.Ast.Node.Assign;

namespace plamp.Ast.NodeComparers.Common;

public class AddAndAssignComparer : IEqualityComparer<AddAndAssignNode>
{
    public bool Equals(AddAndAssignNode x, AddAndAssignNode y)
    {
        if(ReferenceEquals(x, y)) return true;
        if(x == null) return false;
        if(y == null) return false;
        if(x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(AddAndAssignNode obj)
    {
        return HashCode.Combine(obj.Member, obj.Right);
    }
}