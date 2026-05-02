using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <summary>
/// Реализация билдера информации о функции во время компиляции модуля.
/// </summary>
/// <param name="name">Имя функции</param>
/// <param name="args">Список аргументов функции</param>
/// <param name="returnType">Возвращаемый тип функции</param>
/// <param name="moduleName">Имя модуля, в котором объявлена функции</param>
public class BlankFuncInfo(string name, IReadOnlyList<IArgInfo> args, ITypeInfo returnType, string moduleName) : IFnBuilderInfo
{
    private readonly List<IGenericParameterBuilder> _genericParamBuilders = new();

    /// <inheritdoc/>
    public string ModuleName => moduleName;
    
    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public string DefinitionName => name;

    /// <inheritdoc/>
    public IReadOnlyList<IArgInfo> Arguments { get; } = args;
    
    /// <inheritdoc/>
    public ITypeInfo ReturnType => returnType;

    /// <inheritdoc/>
    public bool IsGenericFuncDefinition => _genericParamBuilders.Count != 0;

    /// <inheritdoc/>
    public bool IsGenericFunc => false;

    /// <inheritdoc/>
    public MethodBuilder? MethodBuilder { get; set; }

    /// <summary>
    /// Реализация билдера информации о функции во время компиляции модуля.
    /// </summary>
    /// <param name="name">Имя функции</param>
    /// <param name="args">Список аргументов функции</param>
    /// <param name="returnType">Возвращаемый тип функции</param>
    /// <param name="genericBuilders">Список информации об объявлениях дженерик параметров функции</param>
    /// <param name="moduleName">Имя модуля, в котором объявлена функции</param>
    /// <exception cref="InvalidOperationException">
    /// Если хотя бы один из дженерик параметров имеет тип отличный от дженерик параметра или если хотя бы один из параметров идентичен по имени модулю
    /// или если хотя бы 2 параметра имеют идентичное имя.
    /// </exception>
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
    
    /// <inheritdoc/>
    public MethodInfo AsFunc()
    {
        MethodInfo funcInfo = MethodBuilder ?? throw new NullReferenceException();
        return funcInfo;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericParameters() => _genericParamBuilders;

    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericArguments() => [];

    /// <inheritdoc/>
    public IFnInfo? GetGenericFuncDefinition() => null;

    /// <inheritdoc/>
    public IFnInfo? MakeGenericFunc(IReadOnlyList<ITypeInfo> genericTypeArguments)
    {
        if (_genericParamBuilders.Count == 0) return null;
        return new GenericFuncBuilder(this, genericTypeArguments);
    }
    
    /// <inheritdoc/>
    public IReadOnlyList<IGenericParameterBuilder> GetGenericParameterBuilders() => _genericParamBuilders;

    /// <inheritdoc/>
    public bool Equals(IFnInfo? other)
    {
        if (other is null) return false;
        return other.Name.Equals(Name) && other.ModuleName.Equals(ModuleName);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not IFnInfo info) return false;
        return Equals(info);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Name.GetHashCode(), ModuleName.GetHashCode());
    }
}