using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsBuildingImpl;

namespace plamp.Alternative.SymbolsImpl;

/// <inheritdoc/>
public class FuncInfo : IFnInfo
{
    private readonly MethodInfo _fnInfo;
    private readonly string _moduleName;
    private readonly List<ITypeInfo> _genericParams;

    /// <inheritdoc/>
    public string Name { get; }
    
    /// <inheritdoc/>
    public string DefinitionName { get; }

    /// <inheritdoc/>
    public IReadOnlyList<IArgInfo> Arguments => _fnInfo.GetParameters()
        .Select(x => new ArgInfo(x.Name!, TypeInfo.FromType(x.ParameterType, _moduleName))).ToList();

    /// <inheritdoc/>
    public string ModuleName => _moduleName;

    /// <inheritdoc/>
    public ITypeInfo ReturnType { get; }

    /// <inheritdoc/>
    public bool IsGenericFuncDefinition => _fnInfo.IsGenericMethodDefinition;

    /// <inheritdoc/>
    public bool IsGenericFunc => _fnInfo is { IsGenericMethod: true, IsGenericMethodDefinition: false };

    /// <inheritdoc/>
    public MethodInfo AsFunc() => _fnInfo;

    /// <summary>
    /// Создание экземпляра класса
    /// </summary>
    /// <param name="fnInfo">Информация о методе .net, который надо обернуть в этот класс, не может быть имплементацией дженерик метода</param>
    /// <param name="moduleName">Имя модуля. Не может быть пустым.</param>
    /// <exception cref="InvalidOperationException">Метод является реализацией generic-метода или имя модуля пустое.</exception>
    public FuncInfo(MethodInfo fnInfo, string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            throw new InvalidOperationException("Имя модуля не может быть пустым.");

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
    }

    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericParameters() => _genericParams;

    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericArguments() => [];

    /// <inheritdoc/>
    public IFnInfo? GetGenericFuncDefinition() => null;

    /// <inheritdoc/>
    public IFnInfo? MakeGenericFunc(IReadOnlyList<ITypeInfo> genericTypeArguments)
    {
        if (!_fnInfo.IsGenericMethodDefinition) return null;
        return new GenericFuncBuilder(this, genericTypeArguments);
    }

    /// <inheritdoc/>
    public bool Equals(IFnInfo? other)
    {
        if (other is not FuncInfo fnInfo) return false;
        return fnInfo._fnInfo == _fnInfo;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _fnInfo.GetHashCode();
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not FuncInfo fnInfo) return false;
        return Equals(fnInfo);
    }
} 
