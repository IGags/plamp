using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

/// <summary>
/// Узел ast, обозначающий имя внутри объявления типа
/// </summary>
/// <param name="value">Значение имени</param>
public class TypedefNameNode(string value) : NodeBase
{
    /// <summary>
    /// Значение имени
    /// </summary>
    public string Value = value;
    
    /// <inheritdoc />
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc />
    public override void ReplaceChild(NodeBase child, NodeBase newChild) {}
}