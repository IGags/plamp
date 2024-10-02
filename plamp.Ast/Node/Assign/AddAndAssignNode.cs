using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public record AddAndAssignNode(MemberNode Member, NodeBase Right) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Member;
        yield return Right;
    }
}