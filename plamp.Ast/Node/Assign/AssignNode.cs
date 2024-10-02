using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public record AssignNode(MemberNode Member, NodeBase Right) : BaseAssignNode(Right)
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Member;
        yield return Right;
    }
}