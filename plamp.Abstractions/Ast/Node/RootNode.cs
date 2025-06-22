using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class RootNode : NodeBase
{
    private readonly List<NodeBase> _nodes;
    public IReadOnlyList<NodeBase> Nodes => _nodes;

    public RootNode(List<NodeBase> nodes)
    {
        _nodes = nodes;
    }
    
    public override IEnumerable<NodeBase> Visit()
    {
        return Nodes;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int nodeIndex;
        if (-1 != (nodeIndex = _nodes.IndexOf(child)))
        {
            _nodes[nodeIndex] = newChild;
        }
    }
}