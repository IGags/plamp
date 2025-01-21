using System.Collections.Generic;

namespace plamp.Ast.Node.ControlFlow;

public record ReturnNode(NodeBase ReturnValue) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return ReturnValue;
    }

    public virtual bool Equals(ReturnNode other)
    {
        if(other == null) return false;
        if(ReturnValue == null && other.ReturnValue == null) return true;
        return base.Equals(other);
    }
}