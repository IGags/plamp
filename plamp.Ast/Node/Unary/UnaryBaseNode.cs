using System.Collections.Generic;

namespace plamp.Ast.Node.Unary;

public abstract record UnaryBaseNode(NodeBase Inner) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Inner;
    }
}