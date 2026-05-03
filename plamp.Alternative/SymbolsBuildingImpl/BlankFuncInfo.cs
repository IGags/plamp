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
    
    public string Name
    {
        get
        {
            var generics = _genericParamBuilders.Count == 0
                ? ""
                : $"[{string.Join(", ", _genericParamBuilders.Select(x => x.Name))}]";

            var args = $"({string.Join(", ", Arguments.Select(x => x.Type.Name))})";

            var fullName = name + generics + args;
            return fullName;
        }
    }

    public string DefinitionName => name;

    public IReadOnlyList<IArgInfo> Arguments { get; } = args;
    
    public ITypeInfo ReturnType => returnType;

    public bool IsGenericFuncDefinition => _genericParamBuilders.Count != 0;

    public bool IsGenericFunc => false;

    public MethodBuilder? MethodBuilder { get; set; }

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
        if (_genericParamBuilders.Any(x => x.Equals(builder))) throw new InvalidOperationException("Такой дженерик параметр уже объявлен в функции.");
        _genericParamBuilders.Add(builder);
    }
    
    public MethodInfo AsFunc()
    {
        MethodInfo funcInfo = MethodBuilder ?? throw new NullReferenceException();
        return funcInfo;
    }

    public IReadOnlyList<ITypeInfo> GetGenericParameters() => _genericParamBuilders;

    public IReadOnlyList<ITypeInfo> GetGenericArguments() => [];

    public IFnInfo? GetGenericFuncDefinition() => null;

    public IFnInfo? MakeGenericFunc(IReadOnlyList<ITypeInfo> genericTypeArguments)
    {
        if (_genericParamBuilders.Count == 0) return null;
        return new GenericFuncBuilder(this, genericTypeArguments);
    }
    
    public IReadOnlyList<IGenericParameterBuilder> GetGenericParameterBuilders() => _genericParamBuilders;

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
        return HashCode.Combine(GetType(), Name.GetHashCode(), ModuleName.GetHashCode());
    }
}