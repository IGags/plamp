using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Body;

/// <summary>
/// Узел AST обозначающий условие
/// </summary>
/// <param name="predicate">Условие, определяющее ветвь</param>
/// <param name="ifClause">Действие, если условие истинно</param>
/// <param name="elseClause">Действие, если условие ложно</param>
public class ConditionNode(NodeBase predicate, NodeBase ifClause, NodeBase? elseClause) : NodeBase
{
    /// <summary>
    /// Условие, определяющее ветвь
    /// </summary>
    public NodeBase Predicate { get; private set; } = predicate;
    
    /// <summary>
    /// Действие, если условие истинно
    /// </summary>
    public NodeBase IfClause { get; private set; } = ifClause;
    
    /// <summary>
    /// Действие, если условие ложно
    /// </summary>
    public NodeBase? ElseClause { get; private set; } = elseClause;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Predicate;
        yield return IfClause;
        if (ElseClause != null)
        {
            yield return ElseClause;
        }
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Predicate == child)
        {
            Predicate = newChild;
        }
        else if(IfClause == child)
        {
            IfClause = newChild;
        }
        else if (ElseClause == child)
        {
            ElseClause = newChild;
        }
    }
}