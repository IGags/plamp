using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record ForNode(NodeBase IterableVariable, NodeBase Iterable, BodyNode Body) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return IterableVariable;
        yield return Iterable;
        yield return Body;
    }
}