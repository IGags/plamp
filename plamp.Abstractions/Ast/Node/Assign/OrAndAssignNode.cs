namespace plamp.Abstractions.Ast.Node.Assign;

public class OrAndAssignNode : BaseAssignNode
{
    public OrAndAssignNode(NodeBase left, NodeBase right) : base(left, right)
    {
    }
}