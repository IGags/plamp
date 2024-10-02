using System.Collections.Generic;

namespace plamp.Ast.Node;

public record MemberNode(string MemberName) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }
}