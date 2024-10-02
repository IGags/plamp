using System.Collections.Generic;

namespace plamp.Ast.Node.Binary;

public abstract record BaseBinaryNode(NodeBase Left, NodeBase Right) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Left;
        yield return Right;
    }
}