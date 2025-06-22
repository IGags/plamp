using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.Extensions.Ast.Node;

public class ConditionNode : NodeBase
{
    private readonly List<ClauseNode> _elifClauseList;
    
    public ClauseNode IfClause { get; private set; }
    public IReadOnlyList<ClauseNode> ElifClauseList => _elifClauseList;
    public BodyNode ElseClause { get; private set; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return IfClause;
        foreach (var elif in ElifClauseList)
        {
            yield return elif;
        }

        yield return ElseClause;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int elifClauseIndex;
        if (IfClause == child && newChild is ClauseNode newChildClause)
        {
            IfClause = newChildClause;
        }
        else if (child is ClauseNode childClause
                 && newChild is ClauseNode newChildElifClause
                 && -1 != (elifClauseIndex = _elifClauseList.IndexOf(childClause))
                )
        {
            _elifClauseList[elifClauseIndex] = newChildElifClause;
        }
        else if (ElseClause == child && newChild is BodyNode newChildElseBody)
        {
            ElseClause = newChildElseBody;
        }
    }

    public ConditionNode(ClauseNode ifClause, List<ClauseNode> elifClauseList, BodyNode elseClause)
    {
        IfClause = ifClause;
        _elifClauseList = elifClauseList;
        ElseClause = elseClause;
    }
}