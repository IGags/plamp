using System;
using System.Collections.Generic;
using System.Linq;

namespace plamp.Ast.Node.Body;

public class DefNode 
    : NodeBase
{
    public NodeBase ReturnType { get; }
    public NodeBase Name { get; }
    public List<NodeBase> ParameterList { get; }
    public BodyNode Body { get; }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return ReturnType;
        yield return Name;
        if (ParameterList != null)
        {
            foreach (var parameter in ParameterList)
            {
                yield return parameter;
            }
        }

        yield return Body;
    }

    public DefNode(NodeBase returnType, MemberNode name, List<NodeBase> parameterList, BodyNode body)
    {
        ReturnType = returnType;
        Name = name;
        ParameterList = parameterList ?? [];
        Body = body;
    }
}