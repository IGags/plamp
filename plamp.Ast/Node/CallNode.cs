using System.Collections.Generic;

namespace plamp.Ast.Node;

public record CallNode(NodeBase From, MemberNode Member, List<NodeBase> Args) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return From;
        yield return Member;
        foreach (var arg in Args)
        {
            yield return arg;
        }
    }
}