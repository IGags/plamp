using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Ast.Node;

public class ParameterNode : NodeBase
{
    public NodeBase Type { get; }
    public NodeBase Name { get; }

    public virtual ParameterInfo Symbol { get; } = null;

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
}