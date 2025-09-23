using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

/// <summary>
/// Узел AST получения члена сложного типа.
/// </summary>
/// <param name="from">Значение, из которого нужно получить член</param>
/// <param name="member">Имя члена.</param>
public class MemberAccessNode(NodeBase from, NodeBase member) : NodeBase
{
    /// <summary>
    /// Значение, из которого нужно получить член
    /// </summary>
    public NodeBase From { get; private set; } = from;
    
    /// <summary>
    /// Имя члена.
    /// </summary>
    public NodeBase Member { get; private set; } = member;

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return From;
        yield return Member;
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (From == child) From = newChild;
        else if (Member == child) Member = newChild;
    }
}