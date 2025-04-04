using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class IndexerNode : NodeBase
{
    public NodeBase ToIndex { get; }
    public List<NodeBase> Arguments { get; }

    public IndexerNode(NodeBase toIndex, List<NodeBase> arguments)
    {
        ToIndex = toIndex;
        Arguments = arguments;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return ToIndex;
        foreach (var argument in Arguments)
        {
            yield return argument;
        }
    }
}