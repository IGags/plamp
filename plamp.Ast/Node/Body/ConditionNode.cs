using System.Collections.Generic;

namespace plamp.Ast.Node.Body;

public record ConditionNode(ClauseNode IfClause, List<ClauseNode> ElifClauseList, BodyNode ElseClause) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return IfClause;
        foreach (var elif in ElifClauseList)
        {
            yield return elif;
        }

        yield return ElseClause;
    }
}