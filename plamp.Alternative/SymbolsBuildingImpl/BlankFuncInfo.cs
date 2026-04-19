using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class BlankFuncInfo(string name, IReadOnlyList<IArgInfo> args, ITypeInfo returnType, string moduleName) : IFnBuilderInfo
{
    private readonly List<IGenericParameterBuilder> _genericParamBuilders = new();

    public string ModuleName => moduleName;
    
    public string Name => name;
    
    public IReadOnlyList<IArgInfo> Arguments { get; } = args;
    
    public ITypeInfo ReturnType => returnType;

    public IReadOnlyList<ITypeInfo> GenericParams => _genericParamBuilders;

    public BlankFuncInfo(
        string name,
        IReadOnlyList<IArgInfo> args,
        ITypeInfo returnType,
        IReadOnlyList<IGenericParameterBuilder> genericBuilders,
        string moduleName) : this(name, args, returnType, moduleName)
    {
        foreach (var genericBuilder in genericBuilders)
        {
            AddGenericParameter(genericBuilder);
        }
    }
    
    private void AddGenericParameter(IGenericParameterBuilder builder)
    {
        if (!builder.IsGenericTypeParameter) throw new InvalidOperationException();
        if (!ModuleName.Equals(builder.ModuleName)) throw new InvalidOperationException();
        if (GenericParams.Any(x => x.Equals(builder))) throw new InvalidOperationException("Такой дженерик параметр уже объявлен в функции.");
        _genericParamBuilders.Add(builder);
    }
    
    public MethodInfo AsFunc()
    {
        MethodInfo funcInfo = MethodBuilder ?? throw new NullReferenceException();
        return funcInfo;
    }

    public bool Equals(IFnInfo? other)
    {
        if (other is null) return false;
        return other.Name.Equals(Name) && other.ModuleName.Equals(ModuleName);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not IFnInfo info) return false;
        return Equals(info);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name.GetHashCode(), ModuleName.GetHashCode());
    }

    public MethodBuilder? MethodBuilder { get; set; }
}