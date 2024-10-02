using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record BodyNode(List<NodeBase> InstructionList) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return InstructionList;
    }
}