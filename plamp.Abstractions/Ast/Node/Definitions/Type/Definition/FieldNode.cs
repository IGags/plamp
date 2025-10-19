using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

/// <summary>
/// Узел ast объявления поля типа. Может объявить несколько полей одного типа за раз
/// </summary>
/// <param name="fieldType">Тип поля(ей)</param>
/// <param name="names">Список имён, которые объявляет этот узел</param>
public class FieldNode(TypeNode fieldType, List<FieldNameNode> names) : NodeBase
{
    /// <summary>
    /// Тип поля(ей)
    /// </summary>
    public TypeNode FieldType { get; private set; } = fieldType;

    /// <summary>
    /// Список имён, которые объявляет этот узел
    /// </summary>
    public IReadOnlyList<FieldNameNode> Names => names;

    /// <inheritdoc />
    public override IEnumerable<NodeBase> Visit()
    {
        yield return FieldType;
        foreach (var name in Names)
        {
            yield return name;
        }
    }

    /// <inheritdoc />
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (FieldType == child && newChild is TypeNode newType) FieldType = newType;
        int ix;
        if (child is FieldNameNode name && (ix = names.IndexOf(name)) != -1 && newChild is FieldNameNode newName)
        {
            names[ix] = newName;
        }
    }
}