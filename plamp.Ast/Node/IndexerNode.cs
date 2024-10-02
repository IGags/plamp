using System.Collections.Generic;

namespace plamp.Ast.Node;

public record IndexerNode(NodeBase ToIndex, List<NodeBase> Arguments) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return ToIndex;
        foreach (var argument in Arguments)
        {
            yield return argument;
        }
    }
}