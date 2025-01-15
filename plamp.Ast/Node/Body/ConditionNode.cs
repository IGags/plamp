using System;
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

    public virtual bool Equals(ConditionNode other)
    {
        if (other == null) return false;
        //If-clause always not null
        if (!IfClause.Equals(other.IfClause)) return false;
        if ((ElseClause == null && other.ElseClause != null)
            || (ElseClause != null && other.ElseClause == null)) return false;
        if(ElseClause != null 
           && other.ElseClause != null 
           && !ElseClause.Equals(other.ElseClause)) return false;
        
        for (var i = 0; i < ElifClauseList.Count; i++)
        {
            if(ElifClauseList[i] == null && other.ElifClauseList[i] == null) continue;
            if(!ElifClauseList[i].Equals(other.ElifClauseList[i])) return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), IfClause, ElifClauseList, ElseClause);
    }
}