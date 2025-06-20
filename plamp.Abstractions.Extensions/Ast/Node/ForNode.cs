using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Extensions.Ast.Node;

public class ForNode : NodeBase
{
    public NodeBase IteratorVar { get; private set; }
    public NodeBase TilCondition { get; private set; }
    public NodeBase Counter { get; private set; }
    public NodeBase Body { get; private set; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return IteratorVar;
        yield return TilCondition;
        yield return Counter;
        yield return Body;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (IteratorVar == child)
        {
            IteratorVar = newChild;
        }
        else if (TilCondition == child)
        {
            TilCondition = newChild;
        }
        else if (Counter == child)
        {
            Counter = newChild;
        }
        else if (Body == child)
        {
            Body = newChild;
        }
    }

    public ForNode(NodeBase iteratorVar, NodeBase tilCondition, NodeBase counter, NodeBase body)
    {
        IteratorVar = iteratorVar;
        TilCondition = tilCondition;
        Counter = counter;
        Body = body;
    }
}