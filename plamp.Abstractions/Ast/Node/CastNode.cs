using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class CastNode : NodeBase
{
    public NodeBase ToType { get; }
    public NodeBase Inner { get; }

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
}