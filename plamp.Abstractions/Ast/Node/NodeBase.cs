using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public abstract class NodeBase
{
    public ISymbolOverride SymbolOverride { init; get; }
    
    public abstract IEnumerable<NodeBase> Visit();
}