using System.Collections.Generic;
using System.Linq;

namespace plamp.Ast.Node;

public class EmptyNode : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }
}