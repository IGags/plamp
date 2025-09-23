using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Func;

/// <summary>
/// Узел AST, обозначающий имя функции внутри конструкции вызова функции.
/// </summary>
/// <param name="value">Строковое представление имени</param>
public class FuncCallNameNode(string value) : NodeBase
{
    /// <summary>
    /// Строковое представление имени
    /// </summary>
    public string Value { get; } = value;
    
    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}