using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Ast.Node;

public class ParameterNode : NodeBase
{
    public TypeNode Type { get; private set; }
    public MemberNode Name { get; private set; }

    public ParameterNode(TypeNode type, MemberNode name)
    {
        Type = type;
        Name = name;
    }
    
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        yield return Name;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Type == child && newChild is TypeNode newType)
        {
            Type = newType;
        }
        else if (Name == child && newChild is MemberNode newMember)
        {
            Name = newMember;
        }
    }
}