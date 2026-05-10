using System;
using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <inheritdoc/>
public class BlankArgInfo : IArgInfo
{
    /// <summary>
    /// Создаёт описание аргумента функции.
    /// </summary>
    /// <param name="name">Имя аргумента. Не может быть пустым.</param>
    /// <param name="type">Тип аргумента. Не может быть void.</param>
    /// <exception cref="InvalidOperationException">Имя аргумента пустое или тип аргумента void.</exception>
    public BlankArgInfo(string name, ITypeInfo type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Имя аргумента не может быть пустым.");
        if (SymbolSearchUtility.IsVoid(type))
            throw new InvalidOperationException("Аргумент не может иметь тип void.");

        Name = name;
        Type = type;
    }

    /// <inheritdoc/>
    public string Name { get; }
    
    /// <inheritdoc/>
    public ITypeInfo Type { get; }
    
    /// <inheritdoc/>
    public ParameterInfo AsInfo() => new ParameterInfoImpl(Type.AsType(), Name);
}
