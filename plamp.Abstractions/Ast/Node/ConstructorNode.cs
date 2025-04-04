using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class ConstructorNode : NodeBase
{
    public NodeBase Type { get; }
    public List<NodeBase> Args { get; }

    public ConstructorNode(NodeBase type, List<NodeBase> args)
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