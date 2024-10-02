using System.Collections.Generic;

namespace plamp.Ast.Node;

public abstract record NodeBase
{
    public abstract IEnumerable<NodeBase> Visit();
}