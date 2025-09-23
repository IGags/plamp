using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Ast.Node;

/// <summary>
/// Узел AST обозначающий член внутри функции. (Переменная, параметр, член комплексного типа)
/// </summary>
/// <param name="memberName">Строковое имя члена</param>
public class MemberNode(string memberName) : NodeBase
{
    /// <summary>
    /// Строковое имя члена
    /// </summary>
    public string MemberName { get; } = memberName;

    /// <summary>
    /// Представление члена внутри .net, нужен только для членов сложных типов
    /// </summary>
    public MemberInfo? Symbol { get; protected set; }

    /// <summary>
    /// Установка представления члена типа внутри .net
    /// </summary>
    /// <param name="info">Представление поля типа.</param>
    public void SetMemberInfo(MemberInfo info) => Symbol = info;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => [];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}