using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class MemberAccessNode : NodeBase
{
    public NodeBase From { get; private set; }
    public NodeBase Member { get; private set; }

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

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (From == child)
        {
            From = newChild;
        }
        else if (Member == child)
        {
            Member = newChild;
        }
    }
}