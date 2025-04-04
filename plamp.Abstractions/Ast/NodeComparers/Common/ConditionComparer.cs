using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class ConditionComparer : IEqualityComparer<ConditionNode>
{
    public bool Equals(ConditionNode x, ConditionNode y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(ConditionNode obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.IfClause);
        foreach (var clause in obj.ElifClauseList)
        {
            hashCode.Add(clause);
        }
        hashCode.Add(obj.ElseClause);
        return hashCode.ToHashCode();
    }
}