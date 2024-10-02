using System.Collections.Generic;

namespace plamp.Ast.Node;

public record ParameterNode(TypeNode Type, MemberNode Name) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        yield return Name;
    }
}