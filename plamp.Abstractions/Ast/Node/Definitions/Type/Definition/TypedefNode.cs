using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

/// <summary>
/// Узел ast обозначающий объявление типа. Является классом из c#
/// </summary>
public class TypedefNode(TypedefNameNode name, List<FieldDefNode> fields) : NodeBase
{
    public TypedefNameNode Name { get; private set; } = name;

    public IReadOnlyList<FieldDefNode> Fields => fields;

    /// <inheritdoc />
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Name;
        foreach (var field in Fields)
        {
            yield return field;
        }
    }

    /// <inheritdoc />
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Name == child && newChild is TypedefNameNode newName) Name = newName;
        int ix;
        if (child is FieldDefNode field && (ix = fields.IndexOf(field)) != -1 && newChild is FieldDefNode newField)
        {
            fields[ix] = newField;
        }
    }
}