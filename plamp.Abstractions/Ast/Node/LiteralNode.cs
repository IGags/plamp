using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class LiteralNode : NodeBase
{
    public object Value { get; }
    public Type Type { get; }

    public LiteralNode(object value, Type type)
    {
        Value = value;
        Type = type;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}