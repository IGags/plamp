using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class CastNode : NodeBase
{
    public NodeBase ToType { get; private set; }
    
    public NodeBase Inner { get; private set; }
    
    public Type FromType { get; init; }

    public CastNode(NodeBase toType, NodeBase inner)
    {
        ToType = toType;
        Inner = inner;
    }

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