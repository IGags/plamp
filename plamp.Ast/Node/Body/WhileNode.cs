using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public class WhileNode : NodeBase
{
    public NodeBase Condition { get; }
    
    public BodyNode Body { get; }

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