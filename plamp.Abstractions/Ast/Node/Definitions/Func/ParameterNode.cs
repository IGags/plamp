using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Abstractions.Ast.Node.Definitions.Func;

//TODO: Maybe we want to store parameter data such as ref in out.
public class ParameterNode(TypeNode type, ParameterNameNode name) : NodeBase
{
    public TypeNode Type { get; private set; } = type;
    public ParameterNameNode Name { get; private set; } = name;

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        yield return Name;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Type == child && newChild is TypeNode newType) Type = newType;
        else if (Name == child && newChild is ParameterNameNode newMember) Name = newMember;
    }
}