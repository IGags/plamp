using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.Ast.Node.Definitions;

public class DefNode : NodeBase
{
    private readonly List<ParameterNode> _parameterList;
    
    //Null when void after parsing
    public TypeNode? ReturnType { get; private set; }
    public MemberNode Name { get; private set; }
    public List<ParameterNode> ParameterList => _parameterList;
    public BodyNode Body { get; private set; }

    public override IEnumerable<NodeBase> Visit()
    {
        if (ReturnType != null)
        {
            yield return ReturnType;
        }
        yield return Name;
        foreach (var parameter in ParameterList)
        {
            yield return parameter;
        }

        yield return Body;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int parameterIndex;
        if (ReturnType == child && newChild is TypeNode returnType)
        {
            ReturnType = returnType;
        }
        else if (Name == child && newChild is MemberNode member)
        {
            Name = member;
        }
        else if (child is ParameterNode parameterChild &&
                -1 != (parameterIndex = _parameterList.IndexOf(parameterChild))
                 && newChild is ParameterNode parameterNode)
        {
            _parameterList[parameterIndex] = parameterNode;
        }
        else if (Body == child && newChild is BodyNode newBody)
        {
            Body = newBody;
        }
    }

    public DefNode(TypeNode? returnType, MemberNode name, List<ParameterNode> parameterList, BodyNode body)
    {
        ReturnType = returnType;
        Name = name;
        _parameterList = parameterList;
        Body = body;
    }
}