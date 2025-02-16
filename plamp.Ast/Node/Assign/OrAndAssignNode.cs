using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public class OrAndAssignNode : BaseAssignNode
{
    public NodeBase VariableDefinition { get; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return VariableDefinition;
        yield return Right;
    }

    public OrAndAssignNode(NodeBase variableDefinition, NodeBase right) : base(right)
    {
        VariableDefinition = variableDefinition;
    }
}