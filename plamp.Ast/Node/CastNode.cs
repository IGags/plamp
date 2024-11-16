using System.Collections.Generic;

namespace plamp.Ast.Node;

public record CastNode(NodeBase ToType, NodeBase Inner) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return ToType;
        yield return Inner;
    }
}