using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Ast.Node;

public class ConstructorCallNode : NodeBase
{
    public NodeBase Type { get; private set; }
    public List<NodeBase> Args { get; private set; }

    public virtual ConstructorInfo Symbol { get; init; }

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

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        
    }
}