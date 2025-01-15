using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record BodyNode(List<NodeBase> InstructionList) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return InstructionList;
    }

    public virtual bool Equals(BodyNode other)
    {
        if(other == null) return false;
        if(other.InstructionList == null && InstructionList == null) return true;
        if((other.InstructionList != null && InstructionList == null) 
           || (other.InstructionList == null && InstructionList != null)
           || other.InstructionList!.Count != InstructionList!.Count) return false;
        for (var i = 0; i < InstructionList.Count; i++)
        {
            if(InstructionList[i] == null && other.InstructionList[i] == null) continue;
            if(!InstructionList[i].Equals(other.InstructionList[i])) return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), InstructionList);
    }
}