using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Body;

/// <summary>
/// Узел AST непрерывного списка(блока) инструкций
/// </summary>
public class BodyNode(List<NodeBase> expressionList) : NodeBase
{
    /// <summary>
    /// Список выражений в блоке
    /// </summary>
    public IReadOnlyList<NodeBase> ExpressionList => expressionList;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        return ExpressionList;
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        var index = expressionList.IndexOf(child);
        if(index < 0) return;
        expressionList[index] = newChild;
    }
}