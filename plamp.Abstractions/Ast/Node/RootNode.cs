using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class RootNode : NodeBase
{
    public List<NodeBase> Nodes { get; }

    public RootNode(List<NodeBase> nodes)
    {
        Nodes = nodes;
    }
    
    public override IEnumerable<NodeBase> Visit()
    {
        return Nodes;
    }
}