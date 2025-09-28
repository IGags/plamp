using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

/// <summary>
/// Узел AST обозначающий литерал(константное значение)
/// </summary>
/// <param name="value">Значение литерала</param>
/// <param name="type">Тип этого литерала. Требуется так как иногда типы могут неявно приводиться внутри .net(напр short => int)</param>
public class LiteralNode(object? value, Type type) : NodeBase
{
    /// <summary>
    /// Значение литерала. Null = null
    /// </summary>
    public object? Value { get; } = value;
    
    /// <summary>
    /// Тип этого литерала. Требуется так как иногда типы могут неявно приводиться внутри .net(напр short => int)
    /// </summary>
    public Type Type { get; } = type;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}