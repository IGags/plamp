using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class MemberAccessNode : NodeBase
{
    public NodeBase From { get; }
    public NodeBase Member { get; }

    public MemberAccessNode(NodeBase from, NodeBase member)
    {
        From = from;
        Member = member;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return From;
        yield return Member;
    }
}