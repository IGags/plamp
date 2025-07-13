using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class ParameterComparer : IEqualityComparer<ParameterNode>
{
    public bool Equals(ParameterNode? x, ParameterNode? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(ParameterNode obj)
    {
        return HashCode.Combine(obj.Type, obj.Name);
    }
}