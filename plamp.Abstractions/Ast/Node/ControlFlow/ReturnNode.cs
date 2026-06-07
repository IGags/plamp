using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ControlFlow;

/// <summary>
/// Узел AST обозначающий выход из функции
/// </summary>
/// <param name="returnValues">Список возвращаемых функцией значений. Может быть null, тогда предполагается, что функция имеет возвращаемый тип void</param>
public class ReturnNode(params NodeBase[]? returnValues) : NodeBase
{
    private readonly List<NodeBase> _returnValues = returnValues is null ? [] : [.. returnValues];

    /// <summary>
    /// Список возвращаемых функцией значений. Может быть null, тогда предполагается, что функция имеет возвращаемый тип void
    /// </summary>
    public IReadOnlyList<NodeBase> ReturnValues => _returnValues;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        foreach (var returnValue in ReturnValues)
        {
            yield return returnValue;
        }
    }

    
    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        var ix = _returnValues.IndexOf(child);
        if (ix == -1) return;
        _returnValues[ix] = newChild;
    }
}