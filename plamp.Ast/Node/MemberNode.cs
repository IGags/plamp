using System;
using System.Collections.Generic;

namespace plamp.Ast.Node;

public class MemberNode : NodeBase
{
    public string MemberName { get; }

    public MemberNode(string memberName)
    {
        MemberName = memberName;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }
}