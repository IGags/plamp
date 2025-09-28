using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Type;

/// <summary>
/// Узел AST, обозначающий объявление модуля в текущей единицы компиляции(кодовом файле)
/// </summary>
/// <param name="moduleName">Строковое представление имени молуля</param>
public class ModuleDefinitionNode(string moduleName) : NodeBase
{
    /// <summary>
    /// Строковое представление имени молуля
    /// </summary>
    public string ModuleName { get; } = moduleName;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}