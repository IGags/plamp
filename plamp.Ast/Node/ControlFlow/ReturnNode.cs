using System.Collections.Generic;

namespace plamp.Ast.Node.ControlFlow;

public record ReturnNode(NodeBase ReturnValue) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return ReturnValue;
    }
}