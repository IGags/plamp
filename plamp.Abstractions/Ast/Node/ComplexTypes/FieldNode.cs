using System.Collections.Generic;
using plamp.Abstractions.Symbols;

namespace plamp.Abstractions.Ast.Node.ComplexTypes;

/// <summary>
/// Отображение поля типа внутри тела функции.
/// </summary>
/// <param name="fieldName">Имя поля объекта</param>
public class FieldNode(string fieldName) : NodeBase
{
    /// <summary>
    /// Ссылка на информацию о поле
    /// </summary>
    public IFieldInfo? FieldInfo { get; set; }

    /// <summary>
    /// Имя поля
    /// </summary>
    public string Name { get; } = fieldName;

    /// <inheritdoc/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}