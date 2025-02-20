using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public class AndAndAssignNode : BaseAssignNode
{
    public NodeBase Member { get; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Member;
        yield return Right;
    }

    public AndAndAssignNode(NodeBase member, NodeBase right) : base(right)
    {
        Member = member;
    }
}