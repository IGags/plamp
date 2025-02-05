using System;
using System.Collections.Generic;
using System.Linq;

namespace plamp.Ast.Node;

public record ConstructorNode(NodeBase Type, List<NodeBase> Args) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        foreach (var argument in Args)
        {
            yield return argument;
        }
    }

    public virtual bool Equals(ConstructorNode other)
    {
        if (other == null) return false;
        return Type == other.Type && Args.SequenceEqual(other.Args);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Type);
        foreach (var arg in Args)
        {
            hashCode.Add(arg);
        }
        return hashCode.ToHashCode();
    }
}