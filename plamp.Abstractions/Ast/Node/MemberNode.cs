using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Ast.Node;

public class MemberNode(string memberName) : NodeBase
{
    public string MemberName { get; } = memberName;

    public MemberInfo? Symbol { get; protected set; }

    public void SetMemberInfo(MemberInfo info) => Symbol = info;

    public override IEnumerable<NodeBase> Visit() => [];

    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}