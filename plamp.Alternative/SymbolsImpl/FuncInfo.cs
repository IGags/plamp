using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsImpl;

public class FuncInfo : IFnInfo
{
    private readonly MethodInfo _fnInfo;
    private readonly string _moduleName;

    public string Name { get; }
    
    public IReadOnlyList<IArgInfo> Arguments { get; }

    public ITypeInfo ReturnType { get; }
    
    public IReadOnlyList<ITypeInfo> GenericParams { get; }

    public MethodInfo AsFunc() => _fnInfo;

    public FuncInfo(MethodInfo fnInfo, string moduleName)
    {
        if (fnInfo.IsGenericMethod)
        {
            throw new NotSupportedException();
        }
        
        _fnInfo = fnInfo;
        _moduleName = moduleName;

        GenericParams = fnInfo.IsGenericMethodDefinition 
            ? fnInfo.GetGenericArguments().Select(x => TypeInfo.FromType(x, _moduleName)).ToList() 
            : [];
        
        Name = fnInfo.Name;
        //TODO: Некорректные модули для типов
        ReturnType = TypeInfo.FromType(fnInfo.ReturnType, _moduleName);
        Arguments = fnInfo.GetParameters()
            .Select(x => new ArgInfo(x.Name!, TypeInfo.FromType(x.ParameterType, _moduleName))).ToList();
    }

    public bool Equals(IFnInfo? other)
    {
        if (other is not FuncInfo fnInfo) return false;
        return fnInfo._fnInfo == _fnInfo;
    }

    public string ModuleName => _moduleName;
}