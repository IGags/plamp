using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public class AddAndAssignNode : NodeBase
{
    public NodeBase Member { get; }
    
    public NodeBase Right { get; }

    public AddAndAssignNode(NodeBase member, NodeBase right)
    {
        Member = member;
        Right = right;
    }
    
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Member;
        yield return Member;
    }
}