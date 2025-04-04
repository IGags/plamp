using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Body;

public class ForeachNode : NodeBase, ILoopNode
{
    public NodeBase Iterator { get; }
    public NodeBase Iterable { get; }
    public NodeBase Body { get; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Iterator;
        yield return Iterable;
        yield return Body;
    }

    public ForeachNode(NodeBase iterator, NodeBase iterable, BodyNode body)
    {
        Iterator = iterator;
        Iterable = iterable;
        Body = body;
    }
}