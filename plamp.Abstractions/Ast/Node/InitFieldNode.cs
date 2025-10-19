using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

namespace plamp.Abstractions.Ast.Node;

/// <summary>
/// Узел ast инициализации поля типа
/// </summary>
/// <param name="fieldName">Имя поля типа</param>
/// <param name="value">Выражение значения поля</param>
public class InitFieldNode(FieldNameNode fieldName, NodeBase value) : NodeBase
{
    /// <summary>
    /// Имя поля типа
    /// </summary>
    public FieldNameNode FieldName { get; private set; } = fieldName;

    /// <summary>
    /// Выражение значения поля
    /// </summary>
    public NodeBase Value { get; private set; } = value;
    
    /// <inheritdoc/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return FieldName;
        yield return Value;
    }

    /// <inheritdoc/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (child is FieldNameNode && FieldName == child && newChild is FieldNameNode newName) FieldName = newName;
        if (Value == child) Value = newChild;
    }
}