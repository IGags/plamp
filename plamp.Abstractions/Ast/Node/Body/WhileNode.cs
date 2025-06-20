using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Body;

public class WhileNode : NodeBase
{
    public NodeBase Condition { get; private set; }
    
    public NodeBase Body { get; private set; }

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

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Condition == child)
        {
            Condition = newChild;
        }
        else if (Body == child)
        {
            Body = newChild;
        }
    }
}