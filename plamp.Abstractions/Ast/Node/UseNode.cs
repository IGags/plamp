using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class UseNode : NodeBase
{
    public NodeBase Assembly { get; }

    public UseNode(NodeBase assembly)
    {
        Assembly = assembly;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Assembly;
    }
}