using System;
using System.Collections.Generic;

namespace plamp.Ast.Node;

public record VariableDefinitionNode(NodeBase Type, MemberNode Member) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        yield return Member;
    }

    public virtual bool Equals(VariableDefinitionNode other)
    {
        if (other == null) return false;
        return Type == other.Type && Member == other.Member;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Type, Member);
    }
}