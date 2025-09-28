using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Func;

/// <summary>
/// Узел AST обозначающий имя параметра внутри объявления параметра функции
/// </summary>
/// <param name="value">Строковое представление имени параметра</param>
public class ParameterNameNode(string value) : NodeBase
{
    /// <summary>
    /// Строковое представление имени параметра
    /// </summary>
    public string Value { get; } = value;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}