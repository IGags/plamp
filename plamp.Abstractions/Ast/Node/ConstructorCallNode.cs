using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Ast.Node;

public class ConstructorCallNode(NodeBase type, List<NodeBase> args) : NodeBase
{
    public NodeBase Type { get; } = type;
    public List<NodeBase> Args { get; } = args;

    public ConstructorInfo? Symbol { get; private set; }

    public void SetConstructorInfo(ConstructorInfo info) => Symbol = info;

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        foreach (var argument in Args)
        {
            yield return argument;
        }
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        
    }
}