using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Ast.Node.Body;

namespace plamp.Ast.NodeComparers.Common;

public class BodyComparer : IEqualityComparer<BodyNode>
{
    public bool Equals(BodyNode x, BodyNode y)
    {
        if(ReferenceEquals(x, y)) return true;
        if(x == null) return false;
        if(y == null) return false;
        if(x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(BodyNode obj)
    {
        var hashCode = new HashCode();
        foreach (var instruction in obj.InstructionList)
        {
            hashCode.Add(instruction);
        }

        return hashCode.ToHashCode();
    }
}