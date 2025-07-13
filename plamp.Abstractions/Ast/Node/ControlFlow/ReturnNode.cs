using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ControlFlow;

public class ReturnNode : NodeBase
{
    public NodeBase? ReturnValue { get; private set; }
    
    public ReturnNode(NodeBase? returnValue)
    {
        ReturnValue = returnValue;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        if (ReturnValue != null)
        {
            yield return ReturnValue;
        }
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (ReturnValue == child)
        {
            ReturnValue = newChild;
        }
    }
}