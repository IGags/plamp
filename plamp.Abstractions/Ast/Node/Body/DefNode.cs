using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Body;

public class DefNode : NodeBase
{
    private readonly List<NodeBase> _parameterList;
    
    public NodeBase ReturnType { get; private set; }
    public NodeBase Name { get; private set; }
    public List<NodeBase> ParameterList => _parameterList;
    public BodyNode Body { get; private set; }

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

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int parameterIndex;
        if (ReturnType == child)
        {
            ReturnType = newChild;
        }
        else if (Name == child)
        {
            Name = newChild;
        }
        else if (-1 != (parameterIndex = _parameterList.IndexOf(child)))
        {
            _parameterList[parameterIndex] = newChild;
        }
        else if (Body == child && newChild is BodyNode newBody)
        {
            Body = newBody;
        }
    }

    public DefNode(NodeBase returnType, MemberNode name, List<NodeBase> parameterList, BodyNode body)
    {
        ReturnType = returnType;
        Name = name;
        _parameterList = parameterList ?? [];
        Body = body;
    }
}