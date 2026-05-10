using System;
using System.Reflection;

namespace plamp.Abstractions.Symbols.SymTable;

/// <summary>
/// Объект - описание поля во время компиляции.
/// Реализации, создающие поле с явно переданным именем, должны бросать <see cref="InvalidOperationException"/>,
/// если имя поля пустое или состоит только из пробельных символов.
/// Тип поля не может быть void; такие реализации должны бросать <see cref="InvalidOperationException"/>.
/// Реализации, создающие поле с явно переданным именем модуля, должны бросать <see cref="InvalidOperationException"/>,
/// если имя модуля пустое или состоит только из пробельных символов.
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
    /// <remarks>Тип поля не должен быть void.</remarks>
    public ITypeInfo FieldType { get; }
    
    /// <summary>
    /// Имя поля
    /// </summary>
    /// <remarks>Имя поля не должно быть пустым или состоять только из пробельных символов.</remarks>
    public string Name { get; }
}
