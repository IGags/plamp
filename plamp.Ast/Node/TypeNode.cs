using System.Collections.Generic;

namespace plamp.Ast.Node;

public record TypeNode(string TypeName, List<TypeNode> InnerGenerics) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        return InnerGenerics;
    }
}