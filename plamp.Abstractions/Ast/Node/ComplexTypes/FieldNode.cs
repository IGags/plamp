using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ComplexTypes;

/// <summary>
/// Отображение поля типа внутри тела функции.
/// </summary>
/// <param name="fieldName">Имя поля объекта</param>
public class FieldNode(string fieldName) : NodeBase
{
    /// <summary>
    /// Ссылка на информацию о поле
    /// </summary>
    public ICompileTimeField? Symbol { get; private set; }

    /// <summary>
    /// Установить информацию о поле.
    /// </summary>
    /// <param name="info">Ссылка на информацию о поле</param>
    public void SetInfo(ICompileTimeField info) => Symbol = info;

    /// <summary>
    /// Имя поля
    /// </summary>
    public string Name { get; } = fieldName;

    /// <inheritdoc/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}