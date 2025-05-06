using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;

namespace plamp.Abstractions.Extensions.Ast.Node;

public class MulAndAssignNode : BaseAssignNode
{
    public MulAndAssignNode(NodeBase left, NodeBase right) : base(left, right)
    {
    }
}