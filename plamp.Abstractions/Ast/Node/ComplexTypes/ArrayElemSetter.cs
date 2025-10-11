using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ComplexTypes;

/// <summary>
/// Узел AST, который отвечает за установку значения элемента массива.
/// Располагается в массиве Targets у AssignNode. Предыдущий элемент массива Targets будет присвоен соответствующему элементу Source
/// </summary>
public class ArrayElemSetter : NodeBase
{
    /// <inheritdoc cref="NodeBase.Visit"/>
    public override IEnumerable<NodeBase> Visit() => [];
    
    /// <inheritdoc cref="NodeBase.ReplaceChild"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}