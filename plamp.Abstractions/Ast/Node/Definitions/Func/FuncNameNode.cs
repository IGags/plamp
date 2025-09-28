using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Func;

/// <summary>
/// Узел AST представляющий имя функции в конструкции объявления функции
/// </summary>
/// <param name="value">Строковое представление имени функции</param>
public class FuncNameNode(string value) : NodeBase
{
    /// <summary>
    /// Строковое представление имени функции
    /// </summary>
    public string Value { get; } = value;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) {}
}