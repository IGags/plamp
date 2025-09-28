using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Ast.Node;

/// <summary>
/// Узел AST обозначающий вызов конструктора типа.
/// </summary>
/// <param name="type">Конструируемый тип</param>
/// <param name="args">Список аргументов конструктора</param>
/// <remarks>Скорее всего в следующих версиях пропадёт. Так как язык не подразумевает конструкторы</remarks>
public class ConstructorCallNode(NodeBase type, List<NodeBase> args) : NodeBase
{
    /// <summary>
    /// Конструируемый тип
    /// </summary>
    public NodeBase Type { get; } = type;
    
    /// <summary>
    /// Список аргументов конструктора
    /// </summary>
    public List<NodeBase> Args { get; } = args;

    /// <summary>
    /// Представление конструктора внутри .net, обязан быть во время эмиссии кода в IL
    /// </summary>
    public ConstructorInfo? Symbol { get; private set; }

    /// <summary>
    /// Установка представления конструктора из .net
    /// </summary>
    /// <param name="info">Информация о конструкторе внутри .net</param>
    public void SetConstructorInfo(ConstructorInfo info) => Symbol = info;
    
    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        foreach (var argument in Args)
        {
            yield return argument;
        }
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}