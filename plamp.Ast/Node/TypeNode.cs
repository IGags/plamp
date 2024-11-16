using System.Collections.Generic;

namespace plamp.Ast.Node;

public record TypeNode(NodeBase TypeName, List<NodeBase> InnerGenerics) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return TypeName;
        foreach (var generic in InnerGenerics)
        {
            yield return generic;
        }
    }
}