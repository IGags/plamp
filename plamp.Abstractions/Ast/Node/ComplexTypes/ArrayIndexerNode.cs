using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ComplexTypes;

/// <summary>
/// Узел AST, обозначающий индексатор в массиве. Служит для обозначения [] и того, что между
/// </summary>
/// <param name="indexMember">Выражение внутри индексатора</param>
public class ArrayIndexerNode(NodeBase indexMember) : NodeBase
{
    /// <summary>
    /// Выражение внутри индексатора
    /// </summary>
    public NodeBase IndexMember { get; private set; } = indexMember;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [IndexMember];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (IndexMember == child)
        {
            IndexMember = newChild;
        }
    }
}