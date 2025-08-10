using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Ast.Node;

public class CallNode(NodeBase? from, MemberNode methodName, List<NodeBase> args) : NodeBase
{
    /// <summary>
    /// Null when call local member or member is not defined(implicit module member)
    /// </summary>
    public NodeBase? From { get; private set; } = from;

    public MemberNode MethodName { get; private set; } = methodName;
    public IReadOnlyList<NodeBase> Args => args;

    public MethodInfo? Symbol { get; protected set; }

    public void SetInfo(MethodInfo symbol) => Symbol = symbol;

    public override IEnumerable<NodeBase> Visit()
    {
        if(From != null) yield return From;
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
        else if (-1 != (argIndex = args.IndexOf(child)))
        {
            args[argIndex] = newChild;
        }
    }
}