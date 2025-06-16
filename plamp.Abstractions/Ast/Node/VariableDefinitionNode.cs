using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class VariableDefinitionNode : NodeBase
{
    public NodeBase Type { get; }
    public NodeBase Member { get; }

    public VariableDefinitionNode(NodeBase type, MemberNode member)
    {
        Type = type;
        Member = member;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        yield return Member;
    }
}