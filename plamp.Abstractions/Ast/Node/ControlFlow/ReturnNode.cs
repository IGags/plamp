using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ControlFlow;

/// <summary>
/// Узел AST обозначающий выход из функции
/// </summary>
/// <param name="returnValue">Возвращаемое функцией значение. Может быть null, тогда предполагается, что функция имеет возвращаемый тип void</param>
public class ReturnNode(NodeBase? returnValue) : NodeBase
{
    /// <summary>
    /// Возвращаемое функцией значение. Может быть null, тогда предполагается, что функция имеет возвращаемый тип void
    /// </summary>
    public NodeBase? ReturnValue { get; private set; } = returnValue;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        if (ReturnValue == null) yield break;
        yield return ReturnValue;
    }

    
    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (ReturnValue != child) return;
        ReturnValue = newChild;
    }
}