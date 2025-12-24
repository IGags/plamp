using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Symbols;

namespace plamp.Alternative.SymbolsImpl;

public class FuncInfo : IFnInfo
{
    private readonly MethodInfo _fnInfo;
    public string Name { get; }
    
    public IReadOnlyList<IArgInfo> Arguments { get; }

    public ITypeInfo ReturnType { get; }

    public MethodInfo AsFunc() => _fnInfo;

    public FuncInfo(MethodInfo fnInfo)
    {
        _fnInfo = fnInfo;
        Name = fnInfo.Name;
        ReturnType = new TypeInfo(fnInfo.ReturnType);
        Arguments = fnInfo.GetParameters()
            .Select(x => new ArgInfo(x.Name!, new TypeInfo(x.ParameterType))).ToList();
    }
}