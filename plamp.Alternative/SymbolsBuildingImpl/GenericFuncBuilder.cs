using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <summary>
/// Объект, описывающий имплементацию дженерик функции.
/// </summary>
public class GenericFuncBuilder : IFnInfo
{
    private readonly IFnInfo _definition;
    private readonly IReadOnlyList<ITypeInfo> _genericArguments;

    /// <inheritdoc/>
    public string ModuleName => _definition.ModuleName;

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public string DefinitionName => _definition.DefinitionName;
    
    /// <inheritdoc/>
    public IReadOnlyList<IArgInfo> Arguments { get; }
    
    /// <inheritdoc/>
    public ITypeInfo ReturnType { get; }

    /// <inheritdoc/>
    public bool IsGenericFuncDefinition => false;

    /// <inheritdoc/>
    public bool IsGenericFunc => true;

    /// <summary>
    /// Собирает имплементацию дженерик функции.
    /// </summary>
    /// <param name="definition">Объявление дженерик функции, если функция не дженерик функция - ошибка</param>
    /// <param name="genericArguments">Список дженерик аргументов для реализации функции, если хотя бы один - объявление дженерик типа - ошибка</param>
    /// <exception cref="InvalidOperationException">
    /// Базовая функция не является дженерик объявлением,
    /// или число дженерик аргументов для реализации не равно числу дженерик параметров функции,
    /// или хотя бы 1 дженерик аргумент имплементации функции является объявлением дженерик типа. 
    /// </exception>
    public GenericFuncBuilder(IFnInfo definition, IReadOnlyList<ITypeInfo> genericArguments)
    {
        if (!definition.IsGenericFuncDefinition)
            throw new InvalidOperationException("У закрытой дженерик функции должна быть дженерик функция-объявление от которой она строится.");

        if (genericArguments.Any(x => x.IsGenericTypeDefinition))
            throw new InvalidOperationException("Дженерик функция не может иметь объявление дженерик типа в качестве своего аргумента");
        if (genericArguments.Any(SymbolSearchUtility.IsVoid))
            throw new InvalidOperationException("Дженерик аргумент функции не может иметь тип void.");

        if (definition.GetGenericParameters().Count != genericArguments.Count)
            throw new InvalidOperationException("Число дженерик аргументов у закрытой дженерик функции должно соответствовать числу параметров у её объявления.");
        
        _definition = definition;
        _genericArguments = genericArguments;
        
        var typeMapping = definition.GetGenericParameters()
            .Zip(genericArguments)
            .ToDictionary(x => x.First, x => x.Second);

        Arguments = ImplementArgTypes(definition.Arguments, typeMapping);
        ReturnType = GenericImplementationHelper.ImplementType(definition.ReturnType, typeMapping);
    }

    private IReadOnlyList<IArgInfo> ImplementArgTypes(
        IReadOnlyList<IArgInfo> args,
        IReadOnlyDictionary<ITypeInfo, ITypeInfo> typeMapping)
    {
        var newArgs = new List<BlankArgInfo>();
        foreach (var arg in args)
        {
            var implType = GenericImplementationHelper.ImplementType(arg.Type, typeMapping);
            newArgs.Add(new BlankArgInfo(arg.Name, implType));
        }

        return newArgs;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericParameters() => [];

    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericArguments() => _genericArguments;

    /// <inheritdoc/>
    public IFnInfo GetGenericFuncDefinition() => _definition;

    /// <inheritdoc/>
    public IFnInfo? MakeGenericFunc(IReadOnlyList<ITypeInfo> genericTypeArguments) => null;

    /// <inheritdoc/>
    public MethodInfo AsFunc()
    {
        var argumentTypes = _genericArguments.Select(x => x.AsType()).ToArray();
        return _definition.AsFunc().MakeGenericMethod(argumentTypes);
    }
    
    /// <inheritdoc/>
    public bool Equals(IFnInfo? other)
    {
        if (other is not GenericFuncBuilder genericFunc) return false;
        return ModuleName.Equals(genericFunc.ModuleName) && Name.Equals(genericFunc.Name);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not IFnInfo fnInfo) return false;
        return Equals(fnInfo);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), ModuleName.GetHashCode(), Name.GetHashCode());
    }
}
