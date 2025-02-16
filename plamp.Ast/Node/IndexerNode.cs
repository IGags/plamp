using System;
using System.Collections.Generic;
using System.Linq;

namespace plamp.Ast.Node;

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