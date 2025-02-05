using System;
using System.Collections.Generic;
using System.Linq;

namespace plamp.Ast.Node.Body;

public record ConditionNode(ClauseNode IfClause, List<ClauseNode> ElifClauseList, BodyNode ElseClause) 
    : NodeBase
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
        return IfClause == other.IfClause && ElifClauseList.SequenceEqual(other.ElifClauseList)
            && ElseClause == other.ElseClause;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(IfClause);
        foreach (var clause in ElifClauseList)
        {
            hashCode.Add(clause);
        }
        hashCode.Add(ElseClause);
        return hashCode.ToHashCode();
    }
}