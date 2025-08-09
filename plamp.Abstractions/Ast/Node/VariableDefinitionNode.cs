using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class VariableDefinitionNode(TypeNode? type, MemberNode member) : NodeBase
{
    public TypeNode? Type { get; private set; } = type;
    public MemberNode Member { get; private set; } = member;

    public override IEnumerable<NodeBase> Visit()
    {
        if(Type != null) yield return Type;
        yield return Member;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Type == child && newChild is TypeNode newType) Type = newType;
        else if (Member == child && newChild is MemberNode newMember) Member = newMember;
    }
}