using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Unary;

/// <summary>
/// Базовый тип для всех узлов AST унарных операторов
/// </summary>
/// <param name="inner">Операнд</param>
public abstract class BaseUnaryNode(NodeBase inner) : NodeBase
{
    /// <summary>
    /// Операнд
    /// </summary>
    public NodeBase Inner { get; private set; } = inner;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Inner;
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Inner == child)
        {
            Inner = newChild;
        }
    }
}