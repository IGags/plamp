using System;
using System.Reflection;

namespace plamp.Abstractions.Symbols.SymTable;

/// <summary>
/// Объект - описание поля во время компиляции.
/// </summary>
public interface IFieldInfo : IEquatable<IFieldInfo>
{
    /// <summary>
    /// Превратить этот объект в <see cref="FieldInfo"/> .net runtime
    /// </summary>
    /// <returns>Поле в .net или ошибка, если поля пока нет</returns>
    /// <exception cref="InvalidOperationException">Поле пока не скомпилировано</exception>
    public FieldInfo AsField();
    
    /// <summary>
    /// Получить информацию о типе поля
    /// </summary>
    public ITypeInfo FieldType { get; }
    
    /// <summary>
    /// Имя поля
    /// </summary>
    public string Name { get; }
}