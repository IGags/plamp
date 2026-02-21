using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Symbols.SymTable;

/// <summary>
/// Объект - описание типа во время компиляции.
/// Наследник <see cref="System.Type"/> не используется так как требуется реализация огромного числа свойств.
/// </summary>
public interface ITypeInfo : IEquatable<ITypeInfo>
{
    /// <summary>
    /// Получить список валидных полей, объявленных в типе.
    /// При компиляции типа компилятор вешает специальные атрибуты на поля.
    /// Это оставляет пространство для манёвра(можно держать некоторые поля скрытыми)
    /// </summary>
    public IReadOnlyList<IFieldInfo> Fields { get; }

    /// <summary>
    /// Получение имени типа
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Признак того, является ли тип массивом
    /// </summary>
    public bool IsArrayType { get; }

    /// <summary>
    /// Признак того, является ли тип закрытым дженериком(все параметры заполнены)
    /// </summary>
    public bool IsGenericType { get; }

    /// <summary>
    /// Признак того, является ли тип открытым дженериком(объявлением дженерик типа)
    /// </summary>
    public bool IsGenericTypeDefinition { get; }

    /// <summary>
    /// Признак того, что это тип дженерик параметра
    /// </summary>
    public bool IsGenericTypeParameter { get; }

    /// <summary>
    /// Превратить этот тип в тип .net runtime.
    /// Прямой конверсии нет так как не известно - этот тип существует(модуль откомпилирован) или нет (модуль пока компилируется).
    /// </summary>
    /// <returns>Тип в .net или ошибка, если типа пока нет</returns>
    /// <exception cref="InvalidOperationException">Тип пока не скомпилирован</exception>
    public Type AsType();

    /// <summary>
    /// Создать из текущего типа тип массива, где текущий тип будет его элементом.
    /// </summary>
    /// <returns>Тип массива с текущим типом в качестве элемента</returns>
    public ITypeInfo MakeArrayType();

    /// <summary>
    /// Получить тип элемента, если тип - тип массива
    /// </summary>
    /// <returns>Тип элемента или null, если тип не является типом массива</returns>
    public ITypeInfo? ElementType();
    
    /// <summary>
    /// Получить список типов - дженерик аргументов текущего типа.
    /// </summary>
    /// <returns>Список дженерик аргументов, если тип не дженерик, то возвращается пустой список.</returns>
    public IReadOnlyList<ITypeInfo> GetGenericParameters();

    /// <summary>
    /// Получить объявление дженерик типа из его реализации(получить открытый дженерик из закрытого)
    /// </summary>
    /// <returns>Объявление дженерик типа, если типа не является реализаций дженерик типа, то null</returns>
    public ITypeInfo? GetGenericTypeDefinition();
}