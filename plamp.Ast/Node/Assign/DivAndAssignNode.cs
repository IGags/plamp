using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public record DivAndAssignNode(NodeBase Variable, NodeBase Right) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Variable;
        yield return Right;
    }
}