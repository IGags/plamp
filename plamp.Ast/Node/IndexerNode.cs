using System;
using System.Collections.Generic;
using System.Linq;

namespace plamp.Ast.Node;

public record IndexerNode(NodeBase ToIndex, List<NodeBase> Arguments) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return ToIndex;
        foreach (var argument in Arguments)
        {
            yield return argument;
        }
    }

    public virtual bool Equals(IndexerNode other)
    {
        if (other == null) return false;
        return ToIndex == other.ToIndex && Arguments.SequenceEqual(other.Arguments);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(ToIndex);
        foreach (var argument in Arguments)
        {
            hashCode.Add(argument);
        }
        return hashCode.ToHashCode();
    }
}