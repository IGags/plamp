using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Body;

public class WhileNode : NodeBase, ILoopNode
{
    public NodeBase Condition { get; }
    
    public NodeBase Body { get; }

    public WhileNode(NodeBase condition, BodyNode body)
    {
        Condition = condition;
        Body = body;
    }
    
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Condition;
        yield return Body;
    }
}