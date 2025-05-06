using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Extensions.Ast.Node;

public class ForNode : NodeBase, ILoopNode
{
    public NodeBase IteratorVar { get; }
    public NodeBase TilCondition { get; }
    public NodeBase Counter { get; }
    public NodeBase Body { get; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return IteratorVar;
        yield return TilCondition;
        yield return Counter;
        yield return Body;
    }

    public ForNode(NodeBase iteratorVar, NodeBase tilCondition, NodeBase counter, NodeBase body)
    {
        IteratorVar = iteratorVar;
        TilCondition = tilCondition;
        Counter = counter;
        Body = body;
    }
}