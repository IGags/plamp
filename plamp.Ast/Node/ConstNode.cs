using System;
using System.Collections.Generic;

namespace plamp.Ast.Node;

public class ConstNode : NodeBase
{
    public object Value { get; }
    public Type Type { get; }

    public ConstNode(object value, Type type)
    {
        Value = value;
        Type = type;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }
}