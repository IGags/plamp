using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Binary;

public abstract class BaseBinaryNode : NodeBase
{
    public NodeBase Left { get; }
    public NodeBase Right { get; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Left;
        yield return Right;
    }

    public BaseBinaryNode(NodeBase left, NodeBase right)
    {
        Left = left;
        Right = right;
    }
}