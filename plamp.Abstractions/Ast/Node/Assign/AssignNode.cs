namespace plamp.Abstractions.Ast.Node.Assign;

/// <summary>
/// Узел AST обозначающий операцию присваивания(:= в native диалекте)
/// </summary>
public class AssignNode : BaseAssignNode
{
    public AssignNode(NodeBase left, NodeBase right) : base(left, right)
    {
    }
}