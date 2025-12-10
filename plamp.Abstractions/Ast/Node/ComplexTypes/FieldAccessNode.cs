using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ComplexTypes;

/// <summary>
/// Узел AST получения члена сложного типа.
/// </summary>
/// <param name="from">Значение, из которого нужно получить член</param>
/// <param name="field">Имя поля.</param>
public class FieldAccessNode(NodeBase from, FieldNode field) : NodeBase
{
    /// <summary>
    /// Значение, из которого нужно получить член
    /// </summary>
    public NodeBase From { get; private set; } = from;
    
    /// <summary>
    /// Имя члена.
    /// </summary>
    public FieldNode Field { get; private set; } = field;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return From;
        yield return Field;
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (From == child) From = newChild;
        else if (Field == child && newChild is FieldNode newField) Field = newField;
    }
}