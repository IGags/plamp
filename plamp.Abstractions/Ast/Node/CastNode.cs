using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

/// <summary>
/// Узел AST обозначающий смену типа выражения<br/>
/// Не должен генерироваться парсером напрямую, применяется для неявной смены типа при выводе типов. (напр. fn x() double { return 1L; })
/// </summary>
/// <param name="toType">Целевой тип</param>
/// <param name="inner">Выражение, тип которого требуется сменить</param>
public class CastNode(NodeBase toType, NodeBase inner) : NodeBase
{
    /// <summary>
    /// Целевой тип
    /// </summary>
    public NodeBase ToType { get; private set; } = toType;

    /// <summary>
    /// Выражение, тип которого требуется сменить
    /// </summary>
    public NodeBase Inner { get; private set; } = inner;

    /// <summary>
    /// Тип внутри .net, из которого происходит преобразование. Нужен во время эмиссии в IL.
    /// </summary>
    public Type? FromType { get; protected set; }

    /// <summary>
    /// Установить тип .net, из которого происходит преобразование.
    /// </summary>
    /// <param name="type"></param>
    public void SetFromType(Type type) => FromType = type; 

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return ToType;
        yield return Inner;
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (ToType == child)
        {
            ToType = newChild;
        }
        else if (Inner == child)
        {
            Inner = newChild;
        }
    }
}