using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace plamp.Abstractions;

/// <summary>
/// Таблица символов для конкретного модуля. Используется для определения информации об объявлении типа в том или ином модуле.
/// </summary>
public interface ISymbolTable
{
    /// <summary>
    /// Имя модуля, для которого построена таблица символов
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    /// Попытаться получить объявление типа по ссылке.
    /// </summary>
    /// <param name="typedefReference">Объект-ссылка на объявление типа</param>
    /// <param name="typeInfo">Возвращаемая информация об объявлении типа. Null - если тип не найден.</param>
    /// <returns>True, если тип найден, иначе false.</returns>
    public bool TryGetTypeDefinition(
        CompileTimeType typedefReference, 
        [NotNullWhen(true)]out TypeDefinitionInfo? typeInfo);

    /// <summary>
    /// Попытаться получить объявление функции по ссылке.
    /// </summary>
    /// <param name="functionDefReference">Объект-ссылка на объявление функции</param>
    /// <param name="functionInfo">Возвращаемая информация об объявлении функции. Null - если функция не найдена.</param>
    /// <returns>True, если функция найдена, иначе false.</returns>
    public bool TryGetFunctionDefinition(
        CompileTimeFunction functionDefReference, 
        [NotNullWhen(true)]out FunctionDefinitionInfo? functionInfo);

    /// <summary>
    /// Попытаться получить объявление поля по ссылке.
    /// </summary>
    /// <param name="fieldDefReference">Объект-ссылка на объявление поля типа</param>
    /// <param name="fieldInfo">Возвращаемая информация об объявлении поля. Null - если поле типа не найдено.</param>
    /// <returns>True, если поле типа найдено, иначе false.</returns>
    public bool TryGetFieldDefinition(
        CompileTimeField fieldDefReference,
        [NotNullWhen(true)] out FieldDefinitionInfo? fieldInfo);
    
    /// <summary>
    /// Получить список имён модулей, от которых зависит текущий
    /// </summary>
    public IReadOnlyList<string> GetDependencies();
}