using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Variable;

/// <summary>
/// Узел AST обозначающий имя переменной внутри объявления переменой
/// </summary>
/// <param name="value">Строковое представление имени переменной</param>
public class VariableNameNode(string value) : NodeBase
{
    /// <summary>
    /// Строковое представление имени переменной
    /// </summary>
    public string Value { get; } = value;
    
    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}