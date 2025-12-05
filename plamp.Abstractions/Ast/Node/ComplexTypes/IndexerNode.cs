using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ComplexTypes;

/// <summary>
/// Узел AST, обозначающий индексатор в массиве. Служит для обозначения [] и того, что между
/// </summary>
/// <param name="indexMember">Выражение внутри индексатора</param>
public class IndexerNode(NodeBase from, NodeBase indexMember) : NodeBase
{
    /// <summary>
    /// Выражение внутри индексатора
    /// </summary>
    public NodeBase IndexMember { get; private set; } = indexMember;

    /// <summary>
    /// Индексируемое выражение
    /// </summary>
    public NodeBase From { get; private set; } = from;
    
    public ICompileTimeType? ItemType { get; private set; }
    
    public void SetItemType(ICompileTimeType? itemType) => ItemType = itemType;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [IndexMember, From];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (IndexMember == child)
        {
            IndexMember = newChild;
        }

        if (From == child)
        {
            From = newChild;
        }
    }
}