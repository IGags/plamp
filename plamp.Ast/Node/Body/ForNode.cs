using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record ForNode(VariableDefinitionNode IterableVariable, NodeBase Iterable, BodyNode Body) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return IterableVariable;
        yield return Iterable;
        yield return Body;
    }
}