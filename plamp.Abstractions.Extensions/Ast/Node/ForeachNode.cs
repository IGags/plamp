using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.Extensions.Ast.Node;

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