using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Body;

public class ClauseNode : NodeBase
{
    public NodeBase Predicate { get; }
    public NodeBase Body { get; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Predicate;
        yield return Body;
    }

    public ClauseNode(NodeBase predicate, NodeBase body)
    {
        Predicate = predicate;
        Body = body;
    }
}