using System.Collections.Generic;
using plamp.Abstractions.Symbols;

namespace plamp.Abstractions.Ast.Node.Definitions.Type;

/// <summary>
/// Узел AST обозначающий использование того или иного типа(ссылка на тип)
/// </summary>
/// <param name="typeName">Имя типа</param>
public class TypeNode(TypeNameNode typeName) : NodeBase
{
    /// <summary>
    /// Имя типа
    /// </summary>
    public TypeNameNode TypeName { get; private set; } = typeName;

    /// <summary>
    /// Список объявлений массива от внутреннего ко внешнему
    /// </summary>
    public List<ArrayTypeSpecificationNode> ArrayDefinitions { get; init; } = [];
    
    public ITypeInfo? TypeInfo { get; set; }

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return TypeName;
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (TypeName == child && newChild is TypeNameNode newMember) TypeName = newMember;
    }
}