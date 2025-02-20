using System.Collections.Generic;

namespace plamp.Ast.Node.ControlFlow;

public class BreakNode() : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }
}