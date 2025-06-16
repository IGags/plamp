using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Body;

public class ConditionNode : NodeBase
{
    public NodeBase Predicate { get; }
    public NodeBase IfClause { get; }
    public NodeBase ElseClause { get; }

    public override IEnumerable<NodeBase> Visit()
    {
        throw new System.NotImplementedException();
    }

    public ConditionNode(NodeBase predicate, NodeBase ifClause, NodeBase elseClause)
    {
        Predicate = predicate;
        IfClause = ifClause;
        ElseClause = elseClause;
    }
}