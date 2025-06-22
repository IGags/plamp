using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class VariableDefinitionNode : NodeBase
{
    public NodeBase Type { get; private set; }
    public NodeBase Member { get; private set; }

    public VariableDefinitionNode(NodeBase type, MemberNode member)
    {
        Type = type;
        Member = member;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        yield return Member;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Type == child)
        {
            Type = newChild;
        }
        else if (Member == child)
        {
            Member = newChild;
        }
    }
}