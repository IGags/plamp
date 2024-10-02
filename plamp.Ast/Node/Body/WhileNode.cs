using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record WhileNode(NodeBase Condition, BodyNode Body) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Condition;
        yield return Body;
    }
}