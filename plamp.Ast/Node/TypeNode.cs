using System.Collections.Generic;

namespace plamp.Ast.Node;

public record TypeNode(string TypeName) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }
}