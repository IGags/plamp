namespace plamp.Abstractions.Ast.Node.Assign;

public class AddAndAssignNode : BaseAssignNode
{
    public AddAndAssignNode(NodeBase left, NodeBase right) : base(left, right) { }
}