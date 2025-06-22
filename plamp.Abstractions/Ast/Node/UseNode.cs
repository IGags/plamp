using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class UseNode : NodeBase
{
    public NodeBase Namespace { get; private set; }

    public UseNode(NodeBase @namespace)
    {
        Namespace = @namespace;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Namespace;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Namespace == child)
        {
            Namespace = newChild;
        }        
    }
}