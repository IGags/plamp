using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Extensions.Ast.Node;

public class ClauseNode : NodeBase
{
    public NodeBase Predicate { get; private set; }
    public NodeBase Body { get; private set; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Predicate;
        yield return Body;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Predicate == child)
        {
            Predicate = newChild;
        }
        else if (Body == child)
        {
            Body = newChild;
        }
    }

    public ClauseNode(NodeBase predicate, NodeBase body)
    {
        Predicate = predicate;
        Body = body;
    }
}