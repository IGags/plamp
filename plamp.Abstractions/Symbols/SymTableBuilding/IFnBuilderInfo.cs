using System.Collections.Generic;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.Symbols.SymTableBuilding;

/// <summary>
/// Расширение интерфейса специфичное для создающегося модуля.
/// Описывает объявление функции, добавляя данные в контексте создания текущего модуля.
/// Реализации, создающие функцию с явно переданным именем модуля, должны бросать <see cref="System.InvalidOperationException"/>,
/// если имя модуля пустое или состоит только из пробельных символов.
/// </summary>
public interface IFnBuilderInfo : IFnInfo
{
    /// <summary>
    /// Представление функции в .net
    /// </summary>
    public MethodBuilder? MethodBuilder { get; set; }

    /// <summary>
    /// Возвращает список билдеров дженерик параметров (Если есть)
    /// </summary>
    /// <returns>Список дженерик параметров или [] если таковых нет</returns>
    public IReadOnlyList<IGenericParameterBuilder> GetGenericParameterBuilders();
}
