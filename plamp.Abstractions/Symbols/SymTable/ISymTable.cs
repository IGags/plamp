namespace plamp.Abstractions.Symbols.SymTable;

/// <summary>
/// Объект, определяющий, список членов(символов), которые есть внутри модуля. Их ранящий их описания.
/// Каждый член модуля должен быть уникален в рамках модуля. Не может быть функции и типа у которых одинаковое имя.
/// </summary>
public interface ISymTable
{
    /// <summary>
    /// Имя модуля
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    /// Ищет тип по имени, возвращает тип или null, если тип не найден
    /// </summary>
    /// <param name="name">Имя типа одним словом без объявлений массива или дженерик постфикса. Пустое имя может быть если тип void. Но только для builtin таблицы</param>
    /// <returns>Найденный тип или null, если типа нет</returns>
    public ITypeInfo? FindType(string name);

    /// <summary>
    /// Изет функцию по имени, возвращает функцию или null, если она не найдена
    /// </summary>
    /// <param name="name">Имя функции одним словом без объявлений дженерик постфикса.</param>
    /// <returns>Найденная функция или null, если такой нет</returns>
    public IFnInfo? FindFunc(string name);

    /// <summary>
    /// Содержит ли модуль символ с указанным именем
    /// </summary>
    /// <param name="name">Имя символа, только словом без объявлений массивов и дженериков</param>
    /// <returns>Признак - содержится ли символ в модуле.</returns>
    public bool ContainsSymbol(string name);
}