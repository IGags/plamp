using System.Collections.Generic;

namespace plamp.Ast.Node;

public record CallNode(NodeBase From, List<NodeBase> Args) : NodeBase
{
    public override IEnumerable<NodeBase> Visit()
    {
        yield return From;
        foreach (var arg in Args)
        {
            yield return arg;
        }
    }
}