using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class VariableDefinitionComparer : IEqualityComparer<VariableDefinitionNode>
{
    public bool Equals(VariableDefinitionNode? x, VariableDefinitionNode? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(VariableDefinitionNode obj)
    {
        return HashCode.Combine(obj.Type, obj.Member);
    }
}