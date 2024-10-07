using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public record AndAndAssignNode(NodeBase DefinitionNode, NodeBase Right) : BaseAssignNode(Right)
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return DefinitionNode;
        yield return Right;
    }
}