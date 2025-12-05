using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Abstractions;

/// <summary>
/// Таблица символов для конкретного модуля. Используется для определения информации об объявлении типа в том или ином модуле.<br/>
/// Не потокобезопасна.
/// </summary>
public interface ISymbolTable
{
    /// <summary>
    /// Имя модуля, для которого построена таблица символов
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    /// Попытаться получить ссылку на тип по его имени
    /// </summary>
    /// <param name="typeName">Строковое представление имени типа</param>
    /// <param name="arraySpecs">Число объявлений массивов поверх этого типа</param>
    /// <param name="type">Возвращаемая переменная со ссылкой на тип</param>
    /// <returns>Признак того, был ли найден тип или нет</returns>
    public bool TryGetTypeByName(
        string typeName,
        List<ArrayTypeSpecificationNode> arraySpecs,
        [NotNullWhen(true)] out ICompileTimeType? type);

    /// <summary>
    /// Попытка получить функцию по имени и сигнатуре.
    /// </summary>
    /// <param name="fnName">Имя функции</param>
    /// <param name="signature">Описание типов аргументов</param>
    /// <param name="function">Объект-указатель на информацию об объявлении функции.</param>
    /// <returns>Признак того, была ли получена функция.</returns>
    public bool TryGetFunction(
        string fnName,
        IReadOnlyList<ICompileTimeType> signature,
        [NotNullWhen(true)] out ICompileTimeFunction? function);

    /// <summary>
    /// Получить функции, которые подходят по сигнатуре.
    /// </summary>
    /// <param name="fnName">Имя функции</param>
    /// <param name="signature">Сигнатура функции.</param>
    /// <returns>Список объявлений функции, которые подходят с точностью до неявной конверсии типа.</returns>
    public ICompileTimeFunction[] GetMatchingFunctions(
        string fnName,
        IReadOnlyList<ICompileTimeType> signature);
    
    /// <summary>
    /// Получить список имён модулей, от которых зависит текущий
    /// </summary>
    public IReadOnlyList<ISymbolTable> GetDependencies();
}