using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Ast.Node;

public class CallNode : NodeBase
{
    private readonly List<NodeBase> _args;
    
    public NodeBase From { get; private set; }
    
    public MemberNode MethodName { get; private set; }
    public IReadOnlyList<NodeBase> Args => _args;

    public virtual MethodInfo Symbol { get; protected set; } = null;

    public void SetInfo(MethodInfo symbol) => Symbol = symbol;

    public CallNode(NodeBase from, MemberNode methodName, List<NodeBase> args)
    {
        From = from;
        MethodName = methodName;
        _args = args ?? [];
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return From;
        foreach (var arg in Args)
        {
            yield return arg;
        }
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int argIndex;
        if (From == child)
        {
            From = newChild;
        }
        else if (MethodName == child && newChild is MemberNode member)
        {
            MethodName = member;
        }
        else if (-1 != (argIndex = _args.IndexOf(child)))
        {
            _args[argIndex] = newChild;
        }
    }
}