using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Symbols;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class EmptyFuncInfo(FuncNode funcNode) : IFnInfo
{
    private readonly FuncNode _funcNode = funcNode;
    
    public string Name => _funcNode.FuncName.Value;
    
    public IReadOnlyList<IArgInfo> Arguments { get; } = funcNode.ParameterList.Select(x => new EmptyArgInfo(x)).ToList();
    
    public ITypeInfo ReturnType => _funcNode.ReturnType.TypeInfo ?? throw new NullReferenceException();
    
    public MethodInfo AsFunc()
    {
        MethodInfo funcInfo = _funcNode.Func ?? throw new NullReferenceException();
        return funcInfo;
    }

    public bool Equals(IFnInfo? other)
    {
        if (other is not EmptyFuncInfo fnInfo) return false;
        return fnInfo._funcNode == _funcNode;
    }
}