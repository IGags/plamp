using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Symbols;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class EmptyFuncInfo(FuncNode funcNode) : IFnInfo
{
    public string Name => funcNode.FuncName.Value;
    public IReadOnlyList<IArgInfo> Arguments { get; } = funcNode.ParameterList.Select(x => new EmptyArgInfo(x)).ToList();
    public ITypeInfo ReturnType => funcNode.ReturnType.TypeInfo ?? throw new NullReferenceException();
    public MethodInfo AsFunc() => throw new NotSupportedException();
}