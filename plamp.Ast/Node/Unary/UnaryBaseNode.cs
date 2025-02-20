using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Unary;

public abstract class UnaryBaseNode : NodeBase
{
    public NodeBase Inner { get; }

    public UnaryBaseNode(NodeBase inner)
    {
        Inner = inner;
    }
    
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Inner;
    }
}