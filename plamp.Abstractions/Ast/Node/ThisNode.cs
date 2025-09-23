using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

/// <summary>
/// Узел AST обозначающий ссылку на текущий объект. Используется внутри функций.
/// </summary>
public class ThisNode : NodeBase
{
    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}