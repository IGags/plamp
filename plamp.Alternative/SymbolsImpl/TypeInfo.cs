using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Symbols;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsBuildingImpl;

namespace plamp.Alternative.SymbolsImpl;

/// <inheritdoc/>
public class TypeInfo : ITypeInfo
{
    private readonly Type _type;
    
    /// <inheritdoc/>
    public Type AsType() => _type;

    /// <inheritdoc/>
    public IReadOnlyList<IFieldInfo> Fields { get; }
    
    /// <inheritdoc/>
    public string Name { get; }
    
    /// <inheritdoc/>
    public string ModuleName { get; }

    /// <inheritdoc/>
    public string DefinitionName { get; }

    /// <inheritdoc/>
    public bool IsArrayType => _type.IsArray;

    /// <inheritdoc/>
    public bool IsGenericType => _type is { IsGenericType: true, IsGenericTypeDefinition: false };

    /// <inheritdoc/>
    public bool IsGenericTypeDefinition => _type.IsGenericTypeDefinition;

    /// <inheritdoc/>
    public bool IsGenericTypeParameter => _type.IsGenericMethodParameter;

    private TypeInfo(Type type, string moduleName, string? nameOverride = null)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            throw new InvalidOperationException("Имя модуля не может быть пустым.");

        if (type is { IsGenericType: true, IsGenericTypeDefinition: false }) throw new ArgumentException("Нельзя использовать дженерик тип как тип в таблице символов");
        if (type.IsArray) throw new ArgumentException("Нельзя использовать тип массива как тип в таблице символов");
        ModuleName = moduleName;
        
        _type = type;
        nameOverride ??= type.Name.Split('`', StringSplitOptions.RemoveEmptyEntries)[0];
        DefinitionName = nameOverride;

        if (type.IsGenericTypeDefinition)
        {
            Name = nameOverride + $"[{string.Join(", ", type.GetGenericArguments().Select(t => t.Name))}]";
        }
        else
        {
            Name = nameOverride;
        }
        
        Fields = type.GetFields()
            //TODO: попахивает костылём для ограничения полей из базовых типов .net runtime
            .Where(x => x.GetCustomAttribute<PlampVisibleAttribute>() != null)
            .Select(x => new FldInfo(x, moduleName)).ToList();
    }

    /// <summary>
    /// Сборка объекта из типа .net - единственный публичный способ создать тип.
    /// </summary>
    /// <param name="type">Тип .net на базе которого построить тип</param>
    /// <param name="moduleName">Имя модуля, к которому относится тип.</param>
    /// <param name="nameOverride">Переопределение имени. Не обязательное. Перезаписывает имя для механизмов поиска символов языка.</param>
    /// <exception cref="InvalidOperationException">Имя модуля пустое или состоит только из пробельных символов.</exception>
    /// <returns>Возвращает готовый к использованию объект данного типа</returns>
    public static ITypeInfo FromType(Type type, string moduleName, string? nameOverride = null)
    {
        var arrayCt = 0;
        while (type.IsArray)
        {
            type = type.GetElementType()!;
            arrayCt++;
        }

        ITypeInfo[]? infos = null; 
        if (type is { IsGenericType: true, IsGenericTypeDefinition: false })
        {
            var args = type.GetGenericArguments();
            infos = args.Select(x => FromType(x, moduleName)).ToArray();
            type = type.GetGenericTypeDefinition();
        }

        ITypeInfo defInfo = new TypeInfo(type, moduleName, nameOverride);
        ITypeInfo? genericInfo;
        if (infos != null && (genericInfo = defInfo.MakeGenericType(infos)) != null)
        {
            defInfo = genericInfo;
        }

        for (; 0 < arrayCt; arrayCt--)
        {
            defInfo = defInfo.MakeArrayType();
        }

        return defInfo;
    }
    
    /// <inheritdoc/>
    public ITypeInfo MakeArrayType()
    {
        if (SymbolSearchUtility.IsVoid(this))
            throw new InvalidOperationException("Нельзя создать массив элементов типа void.");

        return new ArrayTypeBuilder(this);
    }

    /// <inheritdoc/>
    public ITypeInfo? MakeGenericType(IReadOnlyList<ITypeInfo> genericTypeArguments)
    {
        if (!_type.IsGenericTypeDefinition) return null;
        return new GenericTypeBuilder(this, genericTypeArguments);
    }

    /// <inheritdoc/>
    public ITypeInfo? ElementType() => null;

    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericParameters()
    {
        if (!IsGenericTypeDefinition) return [];
        return _type.GetGenericArguments().Select(x => new TypeInfo(x, ModuleName)).ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericArguments() => [];

    /// <inheritdoc/>
    public ITypeInfo? GetGenericTypeDefinition()
    {
        if (!IsGenericType) return null;
        var def = _type.GetGenericTypeDefinition();
        return new TypeInfo(def, ModuleName);
    }

    /// <inheritdoc/>
    public bool Equals(ITypeInfo? other)
    {
        if (other is not TypeInfo typ) return false;
        return typ._type == _type;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _type.GetHashCode();
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return _type.Equals(obj);
    }
}
