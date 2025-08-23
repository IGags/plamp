using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Abstractions.Ast.Node.Definitions.Variable;

public class VariableDefinitionNode(TypeNode? type, VariableNameNode name) : NodeBase
{
    public TypeNode? Type { get; private set; } = type;
    public VariableNameNode Name { get; private set; } = name;

    public override IEnumerable<NodeBase> Visit()
    {
        if(Type != null) yield return Type;
        yield return Name;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Type == child && newChild is TypeNode newType) Type = newType;
        else if (Name == child && newChild is VariableNameNode newMember) Name = newMember;
    }
}