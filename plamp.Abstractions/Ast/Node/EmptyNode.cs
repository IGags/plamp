using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

/// <summary>
/// Узел AST представляющий ничего.
/// </summary>
/// <remarks>Возможно будет удалён в следующих версиях.</remarks>
public class EmptyNode : NodeBase
{
    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}