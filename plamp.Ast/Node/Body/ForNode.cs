using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record ForNode(NodeBase Iterator, NodeBase Iterable, BodyNode Body) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Iterator;
        yield return Iterable;
        yield return Body;
    }
}