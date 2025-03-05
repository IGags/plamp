using System;
using System.Collections.Generic;
using System.Linq;

namespace plamp.Ast.Node;

public class CallNode : NodeBase
{
    public NodeBase From { get; }
    public List<NodeBase> Args { get; }

    public CallNode(NodeBase from, List<NodeBase> args)
    {
        From = from;
        Args = args ?? [];
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return From;
        foreach (var arg in Args)
        {
            yield return arg;
        }
    }
}