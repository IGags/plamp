using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsBuildingImpl;

namespace plamp.Alternative.SymbolsImpl;

public class FuncInfo : IFnInfo
{
    private readonly MethodInfo _fnInfo;
    private readonly string _moduleName;
    private readonly List<ITypeInfo> _genericParams;

    public string Name { get; }
    
    public string DefinitionName { get; }

    public IReadOnlyList<IArgInfo> Arguments { get; }

    public string ModuleName => _moduleName;

    public ITypeInfo ReturnType { get; }

    public bool IsGenericFuncDefinition => _fnInfo.IsGenericMethodDefinition;

    public bool IsGenericFunc => _fnInfo is { IsGenericMethod: true, IsGenericMethodDefinition: false };

    public MethodInfo AsFunc() => _fnInfo;

    public FuncInfo(MethodInfo fnInfo, string moduleName)
    {
        if (fnInfo is { IsGenericMethod: true, IsGenericMethodDefinition: false })
            throw new InvalidOperationException("В таблице символов не может быть имплементации дженерик функции");
        
        _fnInfo = fnInfo;
        _moduleName = moduleName;

        _genericParams = fnInfo.IsGenericMethodDefinition 
            ? fnInfo.GetGenericArguments().Select(x => TypeInfo.FromType(x, _moduleName)).ToList() 
            : [];

        var args = $"({string.Join(", ", fnInfo.GetParameters().Select(x => x.ParameterType.Name))})";
        if (fnInfo.IsGenericMethodDefinition)
        {
            Name = fnInfo.Name + $"[{string.Join(", ", fnInfo.GetGenericArguments().Select(x => x.Name))}]" + args;
        }
        else
        {
            Name = fnInfo.Name + args;
        }
        
        DefinitionName = fnInfo.Name;
        //TODO: Некорректные модули для типов
        ReturnType = TypeInfo.FromType(fnInfo.ReturnType, _moduleName);
        Arguments = fnInfo.GetParameters()
            .Select(x => new ArgInfo(x.Name!, TypeInfo.FromType(x.ParameterType, _moduleName))).ToList();
    }

    public IReadOnlyList<ITypeInfo> GetGenericParameters() => _genericParams;

    public IReadOnlyList<ITypeInfo> GetGenericArguments() => [];

    public IFnInfo? GetGenericFuncDefinition() => null;

    public IFnInfo? MakeGenericFunc(IReadOnlyList<ITypeInfo> genericTypeArguments)
    {
        if (!_fnInfo.IsGenericMethodDefinition) return null;
        return new GenericFuncBuilder(this, genericTypeArguments);
    }

    public bool Equals(IFnInfo? other)
    {
        if (other is not FuncInfo fnInfo) return false;
        return fnInfo._fnInfo == _fnInfo;
    }
} 