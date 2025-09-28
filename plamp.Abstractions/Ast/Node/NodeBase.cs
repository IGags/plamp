using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

/// <summary>
/// Базовый общий класс для любого из узлов AST. Определяет основной и общий функционал.<br/>
/// Само AST является неизменяемым для посетителей, поэтому узлы должны быть неизменяемы после создания. (За редкими исключениями) 
/// </summary>
public abstract class NodeBase
{
    /// <summary>
    /// Метод посещения узла. Перечисляет всех его потомков
    /// </summary>
    /// <returns>Перечисление дочерних узлов</returns>
    public abstract IEnumerable<NodeBase> Visit();

    /// <summary>
    /// Метод замены дочернего узла. Не следует использовать внутри посетителей. 
    /// </summary>
    /// <param name="child">Текущий дочерний узел</param>
    /// <param name="newChild">Узел, на который следует заменить данный узел.</param>
    /// <remarks>В дальнейшем возможна смена типа возвращаемого значения на bool</remarks>
    public abstract void ReplaceChild(NodeBase child, NodeBase newChild);
}