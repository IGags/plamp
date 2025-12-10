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
    /// Получить функцию по имени и сигнатуре
    /// </summary>
    /// <param name="fnName">Имя функции</param>
    /// <param name="signature">Сигнатура функции.</param>
    /// <returns>Функция, которая наиболее подходит с точностью до неявной конверсии типа аргумента.</returns>
    public ICompileTimeFunction? GetMatchingFunction(
        string fnName,
        IReadOnlyList<ICompileTimeType?> signature);
    
    /// <summary>
    /// Получить список имён модулей, от которых зависит текущий
    /// </summary>
    public IReadOnlyList<ISymbolTable> GetDependencies();
}