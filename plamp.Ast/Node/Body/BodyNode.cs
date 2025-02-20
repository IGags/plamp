using System;
using System.Collections.Generic;
using System.Linq;

namespace plamp.Ast.Node.Body;

public class BodyNode : NodeBase
{
    public List<NodeBase> InstructionList { get; }

    public override IEnumerable<NodeBase> Visit()
    {
        return InstructionList;
    }

    public BodyNode(List<NodeBase> instructionList)
    {
        InstructionList = instructionList;
    }
}