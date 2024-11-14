using System;
using System.Collections.Generic;

namespace plamp.Ast.Node;

public record ConstNode(object Value, Type Type) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }
}