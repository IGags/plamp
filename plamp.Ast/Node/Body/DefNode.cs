using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record DefNode(TypeNode ReturnType, MemberNode Name, List<ParameterNode> ParameterList, BodyNode Body) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return ReturnType;
        yield return Name;
        foreach (var parameter in ParameterList)
        {
            yield return parameter;
        }

        yield return Body;
    }
}