using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ComplexTypes;

/// <summary>
/// Хранит имя объявления дженерик параметра. Отдельный тип нужен, чтобы корректно подсвечивать ошикбки.
/// </summary>
/// <param name="value">Значение имени дженерик параметра</param>
public class GenericParameterNameNode(string value) : NodeBase
{
    /// <summary>
    /// Значение имени дженерик параметра
    /// </summary>
    public string Value { get; } = value;

    /// <inheritdoc/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}