using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Abstractions.Ast.Node.Definitions.Func;

public class FuncNode(TypeNode? returnType, FuncNameNode funcName, List<ParameterNode> parameterList, BodyNode body) : NodeBase
{
    //Null when void after parsing
    public TypeNode? ReturnType { get; private set; } = returnType;
    public FuncNameNode FuncName { get; private set; } = funcName;
    public List<ParameterNode> ParameterList => parameterList;
    public BodyNode Body { get; private set; } = body;

    public override IEnumerable<NodeBase> Visit()
    {
        if (ReturnType != null)
        {
            yield return ReturnType;
        }
        yield return FuncName;
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
        else if (FuncName == child && newChild is FuncNameNode member)
        {
            FuncName = member;
        }
        else if (child is ParameterNode parameterChild &&
                -1 != (parameterIndex = parameterList.IndexOf(parameterChild))
                 && newChild is ParameterNode parameterNode)
        {
            parameterList[parameterIndex] = parameterNode;
        }
        else if (Body == child && newChild is BodyNode newBody)
        {
            Body = newBody;
        }
    }
}