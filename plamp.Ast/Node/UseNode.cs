using System.Collections.Generic;

namespace plamp.Ast.Node;

public record UseNode(NodeBase Assembly) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Assembly;
    }
}