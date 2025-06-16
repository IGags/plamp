using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Body;

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