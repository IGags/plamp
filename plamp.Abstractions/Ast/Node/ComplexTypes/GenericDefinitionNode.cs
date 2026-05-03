using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ComplexTypes;

/// <summary>
/// Хранит полное описание объявления дженерик параметра типа
/// </summary>
/// <param name="name">Имя дженерик параметра</param>
public class GenericDefinitionNode(GenericParameterNameNode name) : NodeBase
{
    /// <summary>
    /// Имя дженерик параметра
    /// </summary>
    public GenericParameterNameNode Name { get; private set; } = name;
    
    /// <inheritdoc/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Name;
    }

    /// <inheritdoc/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (child == Name && newChild is GenericParameterNameNode parameterName)
        {
            Name = parameterName;
        }
    }
}