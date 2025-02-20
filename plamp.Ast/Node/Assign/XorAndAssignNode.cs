using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public class XorAndAssignNode : BaseAssignNode
{
    public NodeBase VariableDefinition { get; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return VariableDefinition;
        yield return Right;
    }

    public XorAndAssignNode(NodeBase variableDefinition, NodeBase right) : base(right)
    {
        VariableDefinition = variableDefinition;
    }
}