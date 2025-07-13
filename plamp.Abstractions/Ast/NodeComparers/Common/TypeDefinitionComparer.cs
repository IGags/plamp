using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class TypeDefinitionComparer : IEqualityComparer<TypeDefinitionNode>
{
    public bool Equals(TypeDefinitionNode? x, TypeDefinitionNode? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(TypeDefinitionNode obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.Name);
        foreach (var member in obj.Members)
        {
            hashCode.Add(member);
        }

        foreach (var generic in obj.Generics)
        {
            hashCode.Add(generic);
        }
        return hashCode.ToHashCode();
    }
}