using System.Collections.Generic;
using System.Reflection;
using plamp.Abstractions.Ast.Node.Definitions.Func;

namespace plamp.Abstractions.Ast.Node;

/// <summary>
/// Узел AST обозначающий операцию вызова фукции
/// </summary>
/// <param name="from">Объект, чью функцию требуется вызвать. Отсутствие значения означает статическую функцию.</param>
/// <param name="name">Имя вызываемой функции</param>
/// <param name="args">Список аргументов, который передаётся функции</param>
public class CallNode(NodeBase? from, FuncCallNameNode name, List<NodeBase> args) : NodeBase
{
    /// <summary>
    /// Объект, чью функцию требуется вызвать. Отсутствие значения означает статическую функцию.
    /// </summary>
    public NodeBase? From { get; private set; } = from;

    /// <summary>
    /// Имя вызываемой функции
    /// </summary>
    public FuncCallNameNode Name { get; private set; } = name;
    
    /// <summary>
    /// Список аргументов, который передаётся функции
    /// </summary>
    public IReadOnlyList<NodeBase> Args => args;

    /// <summary>
    /// Представление функции внутри .net, добавляется либо во время вывода типов, либо перед самой эмиссией кода функции в IL. Обязан быть во время эмиссии.
    /// </summary>
    public MethodInfo? Symbol { get; protected set; }

    /// <summary>
    /// Установка представления функции внутри .net
    /// </summary>
    /// <param name="symbol">Представление функции из .net</param>
    public void SetInfo(MethodInfo symbol) => Symbol = symbol;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        if(From != null) yield return From;
        foreach (var arg in Args)
        {
            yield return arg;
        }
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int argIndex;
        if (From == child)
        {
            From = newChild;
        }
        else if (Name == child && newChild is FuncCallNameNode member)
        {
            Name = member;
        }
        else if (-1 != (argIndex = args.IndexOf(child)))
        {
            args[argIndex] = newChild;
        }
    }
}