using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Binary;

public abstract class BaseBinaryNode : NodeBase
{
    public NodeBase Left { get; private set; }
    public NodeBase Right { get; private set; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Left;
        yield return Right;
    }

    protected BaseBinaryNode(NodeBase left, NodeBase right)
    {
        Left = left;
        Right = right;
    }
    
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Left == child)
        {
            Left = child;
        }
        else if (Right == child)
        {
            Right = child;
        }
    }
}