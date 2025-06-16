using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.Extensions.Ast.Node;

public class ConditionNode 
    : NodeBase
{
    public ClauseNode IfClause { get; }
    public List<ClauseNode> ElifClauseList { get; }
    public BodyNode ElseClause { get; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return IfClause;
        foreach (var elif in ElifClauseList)
        {
            yield return elif;
        }

        yield return ElseClause;
    }

    public ConditionNode(ClauseNode ifClause, List<ClauseNode> elifClauseList, BodyNode elseClause)
    {
        IfClause = ifClause;
        ElifClauseList = elifClauseList ?? [];
        ElseClause = elseClause;
    }
}