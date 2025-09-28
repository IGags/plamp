using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Type;

/// <summary>
/// Узел AST расширяющий систему типов и обозначающий, что тип является типом массива. Служит для корректного отображения ошибок через таблицу символов.
/// </summary>
public class ArrayTypeSpecificationNode : NodeBase
{
    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) {}
}