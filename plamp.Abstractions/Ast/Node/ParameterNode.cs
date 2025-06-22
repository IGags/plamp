using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Ast.Node;

public class ParameterNode : NodeBase
{
    public NodeBase Type { get; private set; }
    public NodeBase Name { get; private set; }

    public virtual ParameterInfo Symbol { get; init; } = null;

    public ParameterNode(NodeBase type, MemberNode name)
    {
        Type = type;
        Name = name;
    }
    
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        yield return Name;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Type == child)
        {
            Type = newChild;
        }
        else if (Name == child)
        {
            Name = newChild;
        }
    }
}