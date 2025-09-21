using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Type;

public class TypeNode(TypeNameNode typeName) : NodeBase
{
    public TypeNameNode TypeName { get; private set; } = typeName;

    /// <summary>
    /// Список объявлений массива от внутреннего ко внешнему
    /// </summary>
    public List<ArrayTypeSpecificationNode> ArrayDefinitions { get; init; } = [];

    public System.Type? Symbol { get; protected set; }

    public void SetType(System.Type type) => Symbol = type;

    public override IEnumerable<NodeBase> Visit()
    {
        yield return TypeName;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (TypeName == child && newChild is TypeNameNode newMember) TypeName = newMember;
    }
}