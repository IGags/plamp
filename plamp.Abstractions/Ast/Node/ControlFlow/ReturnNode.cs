using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ControlFlow;

public class ReturnNode : NodeBase
{
    public NodeBase ReturnValue { get; }
    
    public ReturnNode(NodeBase returnValue)
    {
        ReturnValue = returnValue;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return ReturnValue;
    }
}