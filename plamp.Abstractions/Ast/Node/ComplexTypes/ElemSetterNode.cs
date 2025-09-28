using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ComplexTypes;

/// <summary>
/// Узел AST обозначающий сохранения элемента массива.<br/>
/// По положению символа в коде повторяет <see cref="ArrayIndexerNode"/> служит, чтобы отследить направление - получение или сохранение элемента массива 
/// </summary>
/// <param name="from">Узел обозначающий массив, в который требуется сохранить элемент</param>
/// <param name="arrayIndexer">Узел индексатора</param>
/// <remarks>Возможно, чтобы упростить семантическую структуру, следует сделать enum определяющий направление индексатора и хранить его внутри <see cref="ArrayIndexerNode"/></remarks>
public class ElemSetterNode(NodeBase from, ArrayIndexerNode arrayIndexer, NodeBase value) : NodeBase
{
    /// <summary>
    /// Тип элемента массива, на который применяется узел. Для корректной компилляции тип обязан присутствовать во время эмиссии кода в IL.
    /// </summary>
    public Type? ItemType { get; private set; }
    
    /// <summary>
    /// Узел обозначающий массив, в который требуется сохранить элемент
    /// </summary>
    public NodeBase From { get; private set; } = from;
    
    /// <summary>
    /// Узел индексатора
    /// </summary>
    public ArrayIndexerNode ArrayIndexer { get; private set; } = arrayIndexer;
    
    /// <summary>
    /// Значение, которое требуется сохранить внутрь массива
    /// </summary>
    public NodeBase Value { get; private set; } = value;

    /// <summary>
    /// Установка типа элемента массива. Происходит во время работы вывода типов. Для корректной компилляции тип обязан присутствовать во время эмиссии кода в IL.
    /// </summary>
    /// <param name="type">Тип элемента массива</param>
    public void SetItemType(Type type)
    {
        ItemType = type;
    }
    
    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return From;
        yield return ArrayIndexer;
        yield return Value;
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (child == From) From = newChild;
        if (child == ArrayIndexer && newChild is ArrayIndexerNode newIndexer) ArrayIndexer = newIndexer;
        if (child == Value) Value = newChild;
    }
}