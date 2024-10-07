using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public record XorAndAssignNode(NodeBase VariableDefinition, NodeBase Right) : BaseAssignNode(Right)
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return VariableDefinition;
        yield return Right;
    }
}