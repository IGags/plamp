using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ControlFlow;

/// <summary>
/// Узел AST обозначающий немедленное завершение цикла, непосредственно внутри тела которого находится инструкция.
/// </summary>
public class BreakNode : NodeBase
{
    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild){}
}