using System;
using System.Reflection;

namespace plamp.Abstractions.Symbols.SymTable;

/// <summary>
/// Информация об параметре функции при компиляции модуля
/// Тип параметра должен соответствовать контракту <see cref="ITypeInfo"/>: реализации типов,
/// созданные с явно переданным именем модуля, бросают <see cref="InvalidOperationException"/> при пустом имени модуля.
/// </summary>
public interface IArgInfo
{
    /// <summary>
    /// Имя параметра
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Тип параметра
    /// </summary>
    public ITypeInfo Type { get; }

    /// <summary>
    /// Превратить информацию в информацию из .net
    /// </summary>
    /// <exception cref="InvalidOperationException">Происходит если параметр ещё не скомпилирован</exception>
    /// <returns>Информация о параметре метода из .net</returns>
    public ParameterInfo AsInfo();
}
