using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Symbols.SymTable;

public interface ISymTable
{
    public string ModuleName { get; }

    /// <summary>
    /// Ищет тип по имени, возвращает тип или null, если тип не найден
    /// </summary>
    /// <param name="name">Имя типа одним словом без объявлений массива или дженерик постфикса. Пустое имя может быть если тип void</param>
    /// <param name="genericsCount">Число дженерик аргументов типа</param>
    /// <exception cref="ArgumentException">Число дженерик аргументов отрицательно.</exception>
    /// <returns>Найденный тип или null, если типа нет</returns>
    public ITypeInfo? FindType(string name, int genericsCount);

    public IReadOnlyList<IFnInfo> FindFuncs(string name);
}