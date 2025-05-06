using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Extensions.Ast.Node;

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