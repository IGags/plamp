using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public abstract class NodeBase
{
    public ISymbolOverride? SymbolOverride { init; get; }
    
    public abstract IEnumerable<NodeBase> Visit();

    public abstract void ReplaceChild(NodeBase child, NodeBase newChild);
}