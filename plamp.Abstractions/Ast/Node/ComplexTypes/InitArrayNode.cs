using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Abstractions.Ast.Node.ComplexTypes;

/// <summary>
/// Узел AST обозначающий создание экземпляра типа массива
/// </summary>
/// <param name="arrayItemType">Узел AST обозначающий тип элемента массива</param>
/// <param name="lengthDefinition">Узел AST обозначающий объявление длины массива</param>
public class InitArrayNode(TypeNode arrayItemType, NodeBase lengthDefinition) : NodeBase
{
    private NodeBase _lengthDefinition = lengthDefinition;
    
    /// <summary>
    /// Узел AST обозначающий тип элемента массива
    /// </summary>
    public TypeNode ArrayItemType { get; private set; } = arrayItemType;

    /// <summary>
    /// Узел AST обозначающий объявление длины массива
    /// </summary>
    public NodeBase LengthDefinition => _lengthDefinition;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return ArrayItemType;
        yield return LengthDefinition;
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (child is TypeNode && newChild is TypeNode newType) ArrayItemType = newType;
        if (child == _lengthDefinition) _lengthDefinition = newChild;
    }
}