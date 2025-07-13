using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Ast.Node;

public class MemberNode : NodeBase
{
    public string MemberName { get; }

    public virtual MemberInfo? Symbol { get; protected set; }

    public void SetMemberInfo(MemberInfo info) => Symbol = info;

    public MemberNode(string memberName)
    {
        MemberName = memberName;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}