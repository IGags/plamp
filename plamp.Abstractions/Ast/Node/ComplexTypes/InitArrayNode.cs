using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Abstractions.Ast.Node.ComplexTypes;

public class InitArrayNode(TypeNode arrayItemType, NodeBase lengthDefinition) : NodeBase
{
    private NodeBase _lengthDefinition = lengthDefinition;
    public TypeNode ArrayItemType { get; private set; } = arrayItemType;

    public NodeBase LengthDefinition => _lengthDefinition;

    public override IEnumerable<NodeBase> Visit()
    {
        yield return ArrayItemType;
        yield return LengthDefinition;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (child is TypeNode && newChild is TypeNode newType) ArrayItemType = newType;
        if (child == _lengthDefinition) _lengthDefinition = newChild;
    }
}