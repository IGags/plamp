using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Базовый класс AST бинарного оператора
/// </summary>
public abstract class BaseBinaryNode(NodeBase left, NodeBase right) : NodeBase
{
    /// <summary>
    /// Левый операнд
    /// </summary>
    public NodeBase Left { get; private set; } = left;

    /// <summary>
    /// Правый операнд
    /// </summary>
    public NodeBase Right { get; private set; } = right;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Left;
        yield return Right;
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Left == child)
        {
            Left = newChild;
        }
        else if (Right == child)
        {
            Right = newChild;
        }
    }
}