using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Ast.Node;

public class CallNode : NodeBase
{
    public NodeBase From { get; }
    
    public NodeBase MethodName { get; }
    public List<NodeBase> Args { get; }

    public virtual MethodInfo Symbol { get; } = null;

    public CallNode(NodeBase from, NodeBase methodName, List<NodeBase> args)
    {
        From = from;
        MethodName = methodName;
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