using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Type;

public class ArrayTypeSpecificationNode : NodeBase
{
    public ArrayTypeSpecificationNode(int dimensions)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(dimensions, 1);
        Dimensions = dimensions;
    }

    public int Dimensions { get; }
    
    public override IEnumerable<NodeBase> Visit() => [];

    public override void ReplaceChild(NodeBase child, NodeBase newChild) {}
}