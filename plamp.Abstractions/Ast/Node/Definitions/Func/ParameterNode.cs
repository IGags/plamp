using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Abstractions.Ast.Node.Definitions.Func;

/// <summary>
/// Узел AST обозначающий объявление параметра внутри объявления функции.
/// </summary>
/// <param name="type">Обозначение типа параметра</param>
/// <param name="name">Обозначение имени параметра</param>
public class ParameterNode(TypeNode type, ParameterNameNode name) : NodeBase
{
    /// <summary>
    /// Обозначение типа параметра
    /// </summary>
    public TypeNode Type { get; private set; } = type;
    
    /// <summary>
    /// Обозначение имени параметра
    /// </summary>
    public ParameterNameNode Name { get; private set; } = name;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        yield return Name;
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Type == child && newChild is TypeNode newType) Type = newType;
        else if (Name == child && newChild is ParameterNameNode newMember) Name = newMember;
    }
}