using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class ThisNode : NodeBase
{
    public ThisNode() { }

    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}