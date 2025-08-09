using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class LiteralNode(object? value, Type type) : NodeBase
{
    public object? Value { get; } = value;
    public Type Type { get; } = type;

    public override IEnumerable<NodeBase> Visit() => [];

    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}