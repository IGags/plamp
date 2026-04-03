using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsImpl;

public class FuncInfo : IFnInfo
{
    private readonly MethodInfo _fnInfo;
    
    public string Name { get; }
    
    public IReadOnlyList<IArgInfo> Arguments { get; }

    public ITypeInfo ReturnType { get; }

    public MethodInfo AsFunc() => _fnInfo;

    public FuncInfo(MethodInfo fnInfo, string moduleName)
    {
        _fnInfo = fnInfo;
        Name = fnInfo.Name;
        ReturnType = TypeInfo.FromType(fnInfo.ReturnType, moduleName);
        Arguments = fnInfo.GetParameters()
            .Select(x => new ArgInfo(x.Name!, TypeInfo.FromType(x.ParameterType, moduleName))).ToList();
    }

    public bool Equals(IFnInfo? other)
    {
        if (other is not FuncInfo fnInfo) return false;
        return fnInfo._fnInfo == _fnInfo;
    }
}