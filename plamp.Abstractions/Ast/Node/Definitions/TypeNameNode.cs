using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions;

/// <summary>
/// Узел AST обозначающий имя типа
/// </summary>
/// <param name="name">Строковое представление имени типа</param>
public class TypeNameNode(string name) : NodeBase
{
    /// <summary>
    /// Строковое представление имени типа
    /// </summary>
    public string Name { get; } = name;
    
    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) {}
}