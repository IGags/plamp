using System.Collections.Generic;

namespace plamp.Abstractions.Symbols.SymTable;

public interface ISymTable
{
    public string ModuleName { get; }
    
    /// <summary>
    /// Ищет типы по имени, возвращает список дженерик перегрузок типа
    /// </summary>
    /// <param name="name">Имя типа одним словом без объявлений массива или дженерик параметров</param>
    /// <returns>Список дженерик перегрузок типа</returns>
    public IReadOnlyList<ITypeInfo> FindTypes(string name);

    public IReadOnlyList<IFnInfo> FindFuncs(string name);
}