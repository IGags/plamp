using System.Collections.Generic;

namespace plamp.Ast.Node;

public record MemberAccessNode(NodeBase From, MemberNode Member) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return From;
        yield return Member;
    }
}