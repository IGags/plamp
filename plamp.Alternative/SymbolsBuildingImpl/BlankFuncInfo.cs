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
public class BlankFuncInfo : IFnBuilderInfo
{
    private readonly List<IGenericParameterBuilder> _genericParamBuilders = new();

    /// <summary>
    /// Создаёт описание функции в контексте строящегося модуля.
    /// </summary>
    /// <param name="name">Имя функции. Не может быть пустым.</param>
    /// <param name="args">Список аргументов функции. Аргументы не могут иметь тип void.</param>
    /// <param name="returnType">Возвращаемый тип функции.</param>
    /// <param name="moduleName">Имя модуля, в котором объявлена функция.</param>
    /// <exception cref="InvalidOperationException">Имя функции пустое, имя модуля пустое, или один из аргументов имеет тип void.</exception>
    public BlankFuncInfo(string name, IReadOnlyList<IArgInfo> args, ITypeInfo returnType, string moduleName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Имя функции не может быть пустым.");
        if (string.IsNullOrWhiteSpace(moduleName))
            throw new InvalidOperationException("Имя модуля не может быть пустым.");
        if (args.Any(x => SymbolSearchUtility.IsVoid(x.Type)))
            throw new InvalidOperationException("Аргумент функции не может иметь тип void.");

        DefinitionName = name;
        Arguments = args;
        ReturnType = returnType;
        ModuleName = moduleName;
    }

    /// <inheritdoc/>
    public string ModuleName { get; }
    
    /// <inheritdoc/>
    public string Name
    {
        get
        {
            var generics = _genericParamBuilders.Count == 0
                ? ""
                : $"[{string.Join(", ", _genericParamBuilders.Select(x => x.Name))}]";

            var args = $"({string.Join(", ", Arguments.Select(x => x.Type.Name))})";

            var fullName = DefinitionName + generics + args;
            return fullName;
        }
    }

    /// <inheritdoc/>
    public string DefinitionName { get; }

    /// <inheritdoc/>
    public IReadOnlyList<IArgInfo> Arguments { get; }
    
    /// <inheritdoc/>
    public ITypeInfo ReturnType { get; }

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
    /// Также происходит, если имя модуля пустое или состоит только из пробельных символов.
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
