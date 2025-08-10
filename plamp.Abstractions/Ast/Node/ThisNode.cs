using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class ThisNode : NodeBase
{
    public override IEnumerable<NodeBase> Visit() => [];

    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}