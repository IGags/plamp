using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class UseNode : NodeBase
{
    public NodeBase Namespace { get; }

    public UseNode(NodeBase @namespace)
    {
        Namespace = @namespace;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Namespace;
    }
}