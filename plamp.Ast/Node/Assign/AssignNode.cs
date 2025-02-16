using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public class AssignNode : BaseAssignNode
{
    public NodeBase Member { get; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Member;
        yield return Right;
    }

    public AssignNode(NodeBase member, NodeBase right) : base(right)
    {
        Member = member;
    }
}