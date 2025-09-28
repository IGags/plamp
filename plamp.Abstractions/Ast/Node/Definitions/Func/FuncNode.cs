using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Abstractions.Ast.Node.Definitions.Func;

/// <summary>
/// Узел AST обозначающий объявление функции.
/// </summary>
/// <param name="returnType">Обозначение типа возвращаемого значения. Может быть null, тогда считается, что функция возвращает void</param>
/// <param name="funcName">Узел, обозначающий имя объявления функции.</param>
/// <param name="parameterList">Список объявлений параметров функции</param>
/// <param name="body">Блок тела функции</param>
public class FuncNode(TypeNode? returnType, FuncNameNode funcName, List<ParameterNode> parameterList, BodyNode body) : NodeBase
{
    /// <summary>
    /// Обозначение типа возвращаемого значения. Может быть null, тогда считается, что функция возвращает void
    /// </summary>
    public TypeNode? ReturnType { get; private set; } = returnType;
    
    /// <summary>
    /// Узел, обозначающий имя объявления функции.
    /// </summary>
    public FuncNameNode FuncName { get; private set; } = funcName;
    
    /// <summary>
    /// Список объявлений параметров функции
    /// </summary>
    public List<ParameterNode> ParameterList => parameterList;
    
    /// <summary>
    /// Блок тела функции
    /// </summary>
    public BodyNode Body { get; private set; } = body;

    /// <inheritdoc cref="NodeBase"/>
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

    /// <inheritdoc cref="NodeBase"/>
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