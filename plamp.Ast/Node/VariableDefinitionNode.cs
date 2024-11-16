using System.Collections.Generic;

namespace plamp.Ast.Node;

public record VariableDefinitionNode(NodeBase Type, MemberNode Member) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        yield return Member;
    }
}