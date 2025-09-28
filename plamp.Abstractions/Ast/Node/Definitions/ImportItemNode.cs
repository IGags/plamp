using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions;

/// <summary>
/// Узел AST обозначающий импорт элемента другого модуля
/// </summary>
/// <param name="name">Оригинальное имя элемента</param>
/// <param name="alias">Псевдоним внутри текущего модуля</param>
public class ImportItemNode(string name, string alias) : NodeBase
{
    /// <summary>
    /// Оригинальное имя элемента
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Псевдоним внутри текущего модуля
    /// </summary>
    public string Alias { get; } = alias;
    
    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}