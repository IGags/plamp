using System;
using System.Collections.Generic;

namespace plamp.Ast.Node.Binary;

public abstract record BaseBinaryNode(NodeBase Left, NodeBase Right) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Left;
        yield return Right;
    }

    public virtual bool Equals(BaseBinaryNode other)
    {
        if(other == null) return false;
        return Left == other.Left && Right == other.Right;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Left, Right);
    }
}