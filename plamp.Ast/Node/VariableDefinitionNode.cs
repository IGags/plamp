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
        if(Type == null && other.Type != null) return false;
        if(Type != null && other.Type == null) return false;
        if(Member == null && other.Member != null) return false;
        if(Member != null && other.Member == null) return false;
        if(Type != null && !Type.Equals(other.Type) ) return false;
        if(Member != null && !Member.Equals(other.Member) ) return false;
        return true;
    }
}