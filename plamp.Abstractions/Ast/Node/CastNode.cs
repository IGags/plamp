using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class CastNode(NodeBase toType, NodeBase inner) : NodeBase
{
    public NodeBase ToType { get; private set; } = toType;

    public NodeBase Inner { get; private set; } = inner;

    /// <summary>
    /// Inference in visitor
    /// </summary>
    public Type? FromType { get; protected set; }

    public void SetFromType(Type type) => FromType = type; 

    public override IEnumerable<NodeBase> Visit()
    {
        yield return ToType;
        yield return Inner;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (ToType == child)
        {
            ToType = newChild;
        }
        else if (Inner == child)
        {
            Inner = newChild;
        }
    }
}