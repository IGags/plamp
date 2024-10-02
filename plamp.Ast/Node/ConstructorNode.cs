using System.Collections.Generic;

namespace plamp.Ast.Node;

public record ConstructorNode(TypeNode Type, List<NodeBase> Args) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        foreach (var argument in Args)
        {
            yield return argument;
        }
    }
}