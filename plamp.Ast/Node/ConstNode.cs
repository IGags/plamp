using System.Collections.Generic;

namespace plamp.Ast.Node;

public record ConstNode(object Value) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }
}