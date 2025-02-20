using System.Collections.Generic;

namespace plamp.Ast.Node;

public abstract class NodeBase
{
    public abstract IEnumerable<NodeBase> Visit();
}