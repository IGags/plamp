using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class GenericFuncBuilder : IFnInfo
{
    private readonly IFnInfo _definition;
    private readonly IReadOnlyList<ITypeInfo> _genericArguments;

    public string ModuleName => _definition.ModuleName;

    public string Name
    {
        get
        {
            var generics = $"[{string.Join(", ", _genericArguments.Select(x => x.Name))}]";
            var args = $"({string.Join(", ", Arguments.Select(x => x.Type.Name))})";

            var fullName = DefinitionName + generics + args;
            return fullName;
        }
    }

    public string DefinitionName => _definition.DefinitionName;
    
    public IReadOnlyList<IArgInfo> Arguments { get; }
    
    public ITypeInfo ReturnType { get; }

    public bool IsGenericFuncDefinition => false;

    public bool IsGenericFunc => true;

    public GenericFuncBuilder(IFnInfo definition, IReadOnlyList<ITypeInfo> genericArguments)
    {
        if (!definition.IsGenericFuncDefinition)
            throw new InvalidOperationException("У закрытой дженерик функции должна быть дженерик функция-объявление от которой она строится.");

        if (genericArguments.Any(x => x.IsGenericTypeDefinition))
            throw new InvalidOperationException("Дженерик функция не может иметь объявление дженерик типа в качестве своего аргумента");

        if (definition.GetGenericParameters().Count != genericArguments.Count)
            throw new InvalidOperationException("Число дженерик аргументов у закрытой дженерик функции должно соответствовать числу параметров у её объявления.");
        
        _definition = definition;
        _genericArguments = genericArguments;
        
        var typeMapping = definition.GetGenericParameters()
            .Zip(genericArguments)
            .ToDictionary(x => x.First, x => x.Second);

        Arguments = ImplementArgTypes(definition.Arguments, typeMapping);
        ReturnType = GenericTypeBuilder.ImplementType(definition.ReturnType, typeMapping);
    }

    private IReadOnlyList<IArgInfo> ImplementArgTypes(
        IReadOnlyList<IArgInfo> args,
        IReadOnlyDictionary<ITypeInfo, ITypeInfo> typeMapping)
    {
        var newArgs = new List<BlankArgInfo>();
        foreach (var arg in args)
        {
            var implType = GenericTypeBuilder.ImplementType(arg.Type, typeMapping);
            newArgs.Add(new BlankArgInfo(arg.Name, implType));
        }

        return newArgs;
    }

    public IReadOnlyList<ITypeInfo> GetGenericParameters() => [];

    public IReadOnlyList<ITypeInfo> GetGenericArguments() => _genericArguments;

    public IFnInfo GetGenericFuncDefinition() => _definition;

    public IFnInfo? MakeGenericFunc(IReadOnlyList<ITypeInfo> genericTypeArguments) => null;

    public MethodInfo AsFunc()
    {
        var argumentTypes = _genericArguments.Select(x => x.AsType()).ToArray();
        return _definition.AsFunc().MakeGenericMethod(argumentTypes);
    }
    
    public bool Equals(IFnInfo? other)
    {
        if (other is not GenericFuncBuilder genericFunc) return false;
        return ModuleName.Equals(genericFunc.ModuleName) && Name.Equals(genericFunc.Name);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not IFnInfo fnInfo) return false;
        return Equals(fnInfo);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), ModuleName.GetHashCode(), Name.GetHashCode());
    }
}