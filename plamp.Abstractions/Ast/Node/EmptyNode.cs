﻿using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class EmptyNode : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}