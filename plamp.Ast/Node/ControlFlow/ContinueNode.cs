using System.Collections.Generic;

namespace plamp.Ast.Node.ControlFlow;

public record ContinueNode : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }
}