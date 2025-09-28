using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Body;

/// <summary>
/// Узел AST обозначающий цикл с предусловием
/// </summary>
public class WhileNode(NodeBase condition, BodyNode body) : NodeBase
{
    /// <summary>
    /// Предикат, который определяет возможность совершения итерации
    /// </summary>
    public NodeBase Condition { get; private set; } = condition;

    /// <summary>
    /// Тело цикла
    /// </summary>
    public NodeBase Body { get; private set; } = body;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Condition;
        yield return Body;
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Condition == child)
        {
            Condition = newChild;
        }
        else if (Body == child)
        {
            Body = newChild;
        }
    }
}