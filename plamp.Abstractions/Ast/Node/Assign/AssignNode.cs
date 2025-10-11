using System.Collections.Generic;
using System.Linq;

namespace plamp.Abstractions.Ast.Node.Assign;

/// <summary>
/// Узел AST обозначающий операцию присваивания(:= в native диалекте)
/// </summary>
public class AssignNode(List<NodeBase> targets, List<NodeBase> sources) : NodeBase
{
    /// <summary>
    /// Список целей присвоения
    /// </summary>
    public IReadOnlyList<NodeBase> Targets { get; } = targets;

    /// <summary>
    /// Список источников присвоения
    /// </summary>
    public IReadOnlyList<NodeBase> Sources { get; } = sources;
    
    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        foreach (var source in Sources)
        {
            yield return source;
        }

        foreach (var target in Targets.AsEnumerable().Reverse())
        {
            yield return target;
        }
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int ix;
        if ((ix = targets.IndexOf(child)) != -1)
        {
            targets[ix] = newChild;
        }
        else if ((ix = sources.IndexOf(child)) != -1)
        {
            sources[ix] = newChild;
        }
    }
}