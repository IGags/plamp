using System.Collections.Generic;
using System.Reflection.Emit;

namespace plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

/// <summary>
/// Узел ast объявления поля типа. Может объявить несколько полей одного типа за раз
/// </summary>
/// <param name="fieldType">Тип поля(ей)</param>
/// <param name="name">Имя объявленного поля.</param>
public class FieldDefNode(TypeNode fieldType, FieldNameNode name) : NodeBase
{
    public FieldBuilder? Field { get; set; }
    
    /// <summary>
    /// Тип поля(ей)
    /// </summary>
    public TypeNode FieldType { get; private set; } = fieldType;

    /// <summary>
    /// Имя объявленного поля.
    /// </summary>
    public FieldNameNode Name { get; private set; } = name;
    
    /// <inheritdoc />
    public override IEnumerable<NodeBase> Visit()
    {
        yield return FieldType;
        yield return Name;
    }

    /// <inheritdoc />
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (FieldType == child && newChild is TypeNode newType) FieldType = newType;
        if (Name == child && newChild is FieldNameNode newName) Name = newName;
    }
}