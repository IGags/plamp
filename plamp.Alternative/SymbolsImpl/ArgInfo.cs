using System;
using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsImpl;

/// <inheritdoc/>
public class ArgInfo : IArgInfo
{
    /// <summary>
    /// Создаёт описание runtime-аргумента функции.
    /// </summary>
    /// <param name="name">Имя аргумента. Не может быть пустым.</param>
    /// <param name="typeInfo">Тип аргумента. Не может быть void.</param>
    /// <exception cref="InvalidOperationException">Имя аргумента пустое или тип аргумента void.</exception>
    public ArgInfo(string name, ITypeInfo typeInfo)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Имя аргумента не может быть пустым.");
        if (SymbolSearchUtility.IsVoid(typeInfo))
            throw new InvalidOperationException("Аргумент не может иметь тип void.");

        Name = name;
        Type = typeInfo;
    }

    public string Name { get; }

    public ITypeInfo Type { get; }
    
    public ParameterInfo AsInfo() => new ParameterInfoImpl(Type.AsType(), Name);
}
