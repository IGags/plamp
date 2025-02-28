using plamp.Ast.Node.Binary;

namespace plamp.Ast.Node.Assign;

/// <summary>
/// Basic class-mark for assignment expressions
/// </summary>
public abstract class BaseAssignNode : BaseBinaryNode
{
    protected BaseAssignNode(NodeBase left, NodeBase right) : base(left, right) { }
}