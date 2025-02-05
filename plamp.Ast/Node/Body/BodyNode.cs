using System;
using System.Collections.Generic;
using System.Linq;

namespace plamp.Ast.Node.Body;

public record BodyNode(List<NodeBase> InstructionList) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return InstructionList;
    }

    public virtual bool Equals(BodyNode other)
    {
        return other != null && InstructionList.SequenceEqual(other.InstructionList);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        foreach (var instruction in InstructionList)
        {
            hashCode.Add(instruction);
        }

        return hashCode.ToHashCode();
    }
}