using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record ClauseNode(NodeBase Predicate, BodyNode Body) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Predicate;
        yield return Body;
    }
}