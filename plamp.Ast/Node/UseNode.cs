using System.Collections.Generic;

namespace plamp.Ast.Node;

public record UseNode(MemberNode Assembly) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Assembly;
    }
}