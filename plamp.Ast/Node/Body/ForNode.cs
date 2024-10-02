using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record ForNode(VariableDefinitionNode IterableVariable, NodeBase Iterator, BodyNode Body) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return IterableVariable;
        yield return Iterator;
        yield return Body;
    }
}