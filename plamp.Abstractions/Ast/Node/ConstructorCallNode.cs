using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Ast.Node;

public class ConstructorCallNode : NodeBase
{
    public NodeBase Type { get; }
    public List<NodeBase> Args { get; }

    public ConstructorInfo Symbol { get; } = null;

    public ConstructorCallNode(NodeBase type, List<NodeBase> args)
    {
        Type = type;
        Args = args;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        foreach (var argument in Args)
        {
            yield return argument;
        }
    }
}