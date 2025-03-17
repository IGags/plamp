using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace plamp.Ast.Node;

public class ParameterNode : NodeBase
{
    public NodeBase Type { get; }
    public NodeBase Name { get; }

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