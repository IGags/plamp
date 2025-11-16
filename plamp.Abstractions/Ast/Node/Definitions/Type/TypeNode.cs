using System.Collections.Generic;

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

    /// <summary>
    /// Ссылка на информацию об объявлении типа.
    /// </summary>
    public CompileTimeType? TypedefRef { get; protected set; }

    /// <summary>
    /// Установка ссылки на объявление внутри таблицы символов
    /// </summary>
    /// <param name="type">Ссылка на объявление типа</param>
    public void SetTypeRef(CompileTimeType type) => TypedefRef = type;

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