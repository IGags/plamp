using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public record AddAndAssignNode(NodeBase Member, NodeBase Right) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Member;
        yield return Right;
    }
}