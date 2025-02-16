using System.Collections.Generic;

namespace plamp.Ast.Node.Assign;

public class SubAndAssignNode : NodeBase
{
    public NodeBase Variable { get; }
    public NodeBase Right { get; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Variable;
        yield return Right;
    }

    public SubAndAssignNode(NodeBase variable, NodeBase right)
    {
        Variable = variable;
        Right = right;
    }
}