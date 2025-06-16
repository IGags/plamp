using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class ThisNode : NodeBase
{
    public ThisNode() { }

    public override IEnumerable<NodeBase> Visit()
    {
        yield break;
    }
}