using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Extensions.Ast.Node;

public class IndexerNode : NodeBase
{
    private readonly List<NodeBase> _arguments;
    
    public NodeBase ToIndex { get; private set; }
    public IReadOnlyList<NodeBase> Arguments => _arguments;

    public IndexerNode(NodeBase toIndex, List<NodeBase> arguments)
    {
        ToIndex = toIndex;
        _arguments = arguments;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return ToIndex;
        foreach (var argument in Arguments)
        {
            yield return argument;
        }
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int argumentIndex;
        if (ToIndex == child)
        {
            ToIndex = newChild;
        }
        else if (-1 != (argumentIndex = _arguments.IndexOf(child)))
        {
            _arguments[argumentIndex] = newChild;
        }
    }
}