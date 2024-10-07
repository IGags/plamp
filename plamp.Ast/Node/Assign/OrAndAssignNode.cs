using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public record OrAndAssignNode(NodeBase VariableDefinition, NodeBase Right) : BaseAssignNode(Right)
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return VariableDefinition;
        yield return Right;
    }
}