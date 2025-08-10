using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

//TODO: Maybe we want to store parameter data such as ref in out.
public class ParameterNode(TypeNode type, MemberNode name) : NodeBase
{
    public TypeNode Type { get; private set; } = type;
    public MemberNode Name { get; private set; } = name;

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        yield return Name;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Type == child && newChild is TypeNode newType) Type = newType;
        else if (Name == child && newChild is MemberNode newMember) Name = newMember;
    }
}