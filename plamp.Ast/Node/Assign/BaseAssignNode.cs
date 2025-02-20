namespace plamp.Ast.Node.Assign;

public abstract class BaseAssignNode : NodeBase
{
    public NodeBase Right { get; }

    public BaseAssignNode(NodeBase right)
    {
        Right = right;
    }
}