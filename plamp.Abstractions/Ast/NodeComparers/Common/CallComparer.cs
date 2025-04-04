using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Ast.NodeComparers.Common;

public class CallComparer : IEqualityComparer<CallNode>
{
    public bool Equals(CallNode x, CallNode y)
    {
        if(ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return true;
    }

    public int GetHashCode(CallNode obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.From);
        foreach (var arg in obj.Args)
        {
            hashCode.Add(arg);
        }
        return hashCode.ToHashCode();
    }
}