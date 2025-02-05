using System;
using System.Collections.Generic;
using System.Linq;

namespace plamp.Ast.Node;

public record CallNode(NodeBase From, List<NodeBase> Args) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return From;
        foreach (var arg in Args)
        {
            yield return arg;
        }
    }

    public virtual bool Equals(CallNode other)
    {
        if (other == null) return false;
        return From == other.From && Args.SequenceEqual(other.Args);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(From);
        foreach (var arg in Args)
        {
            hashCode.Add(arg);
        }
        return hashCode.ToHashCode();
    }
}