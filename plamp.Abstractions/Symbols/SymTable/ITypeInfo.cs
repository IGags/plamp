using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Symbols.SymTable;

/// <summary>
/// Объект - описание типа во время компиляции.
/// Наследник <see cref="System.Type"/> не используется так как требуется реализация огромного числа свойств.
/// </summary>
public interface ITypeInfo : IModuleMember, IEquatable<ITypeInfo>
{
    /// <summary>
    /// Получить список валидных полей, объявленных в типе.
    /// При компиляции типа компилятор вешает специальные атрибуты на поля.
    /// Это оставляет пространство для манёвра(можно держать некоторые поля скрытыми)
    /// </summary>
    public IReadOnlyList<IFieldInfo> Fields { get; }

    /// <summary>
    /// Получение имени типа, уникально в рамках модуля. Это значит, что два объявления типа идентичны, если идентичны их имена и модули, в которых они объявлены.
    /// Имя типа массива и имплементации дженерика обязано отличаться от имени объявления
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Имя типа без объявлений массивов и дженерик параметров, может быть неуникально(например для типа массива).
    /// Если тип - дженерик параметр, то вернуть имя дженерик параметра
    /// </summary>
    public string DefinitionName { get; }

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
    /// Собрать из текущего типа закрытый дженерик тип.
    /// Работает только в случае, если текущий тип - объявление дженерика
    /// </summary>
    /// <param name="genericTypeArguments">Список аргументов для дженерик типа</param>
    /// <exception cref="InvalidOperationException">Число дженерик аргументов не совпадает или некоторые из них - объявления дженерик типов</exception>
    /// <returns>Новый тип или null, если исходный тип не объявление дженерик типа</returns>
    public ITypeInfo? MakeGenericType(IReadOnlyList<ITypeInfo> genericTypeArguments);

    /// <summary>
    /// Получить тип элемента, если тип - тип массива
    /// </summary>
    /// <returns>Тип элемента или null, если тип не является типом массива</returns>
    public ITypeInfo? ElementType();
    
    /// <summary>
    /// Получить список типов - дженерик параметров текущего типа.
    /// </summary>
    /// <returns>Список дженерик аргументов, если тип не объявление дженерика, то возвращается пустой список.</returns>
    public IReadOnlyList<ITypeInfo> GetGenericParameters();
    
    /// <summary>
    /// Получить список типов, которые являются аргументами дженерик типа.
    /// </summary>
    /// <returns>Список дженерик аргументов у текущего типа. Если тип не является закрытым дженерик типом, то пустой список.</returns>
    public IReadOnlyList<ITypeInfo> GetGenericArguments();

    /// <summary>
    /// Получить объявление дженерик типа из его реализации(получить открытый дженерик из закрытого)
    /// </summary>
    /// <returns>Объявление дженерик типа, если типа не является реализаций дженерик типа, то null</returns>
    public ITypeInfo? GetGenericTypeDefinition();
}