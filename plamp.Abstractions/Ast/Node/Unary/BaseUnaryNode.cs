﻿using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Unary;

public abstract class BaseUnaryNode : NodeBase
{
    public NodeBase Inner { get; private set; }

    public BaseUnaryNode(NodeBase inner)
    {
        Inner = inner;
    }
    
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Inner;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Inner == child)
        {
            Inner = newChild;
        }
    }
}