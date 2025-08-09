using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class DefComparer : IEqualityComparer<FuncNode>
{
    public bool Equals(FuncNode? x, FuncNode? y)
    {
        if(ReferenceEquals(x, y)) return true;
        if(x == null) return false;
        if(y == null) return false;
        if(x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(FuncNode obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.ReturnType);
        hashCode.Add(obj.Name);
        foreach (var parameter in obj.ParameterList)
        {
            hashCode.Add(parameter);
        }
        hashCode.Add(obj.Body);
        return hashCode.ToHashCode();
    }
}