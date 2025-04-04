using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Unary;

public abstract class BaseUnaryNode : NodeBase
{
    public NodeBase Inner { get; }

    public BaseUnaryNode(NodeBase inner)
    {
        Inner = inner;
    }
    
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Inner;
    }
}