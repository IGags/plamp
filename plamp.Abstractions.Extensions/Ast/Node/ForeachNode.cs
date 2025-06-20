using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.Extensions.Ast.Node;

public class ForeachNode : NodeBase
{
    public NodeBase Iterator { get; private set; }
    public NodeBase Iterable { get; private set; }
    public NodeBase Body { get; private set; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Iterator;
        yield return Iterable;
        yield return Body;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Iterator == child)
        {
            Iterator = newChild;
        }
        else if (Iterable == child)
        {
            Iterable = newChild;
        }
        else if (Body == child)
        {
            Body = newChild;
        }
    }

    public ForeachNode(NodeBase iterator, NodeBase iterable, BodyNode body)
    {
        Iterator = iterator;
        Iterable = iterable;
        Body = body;
    }
}