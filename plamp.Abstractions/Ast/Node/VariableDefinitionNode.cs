using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class VariableDefinitionNode : NodeBase
{
    public TypeNode Type { get; private set; }
    public MemberNode Member { get; private set; }

    public VariableDefinitionNode(TypeNode type, MemberNode member)
    {
        Type = type;
        Member = member;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        yield return Member;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Type == child && newChild is TypeNode newType)
        {
            Type = newType;
        }
        else if (Member == child && newChild is MemberNode newMember)
        {
            Member = newMember;
        }
    }
}