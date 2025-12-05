using System;
using System.Collections.Generic;

namespace plamp.Abstractions;

/// <summary>
/// Класс, определяющий ссылку на объявление функции.
/// Нужен так как не всегда во время компиляции можно ассоциировать функцию с её скомпилированным значением из clr.
/// Для каждой функции существует в единственном экземпляре в таблице символов.
/// </summary>
public interface ICompileTimeFunction : IEquatable<ICompileTimeFunction>
{
    /// <summary>
    /// Таблица описания модуля, в котором объявлен тип.
    /// </summary>
    public ISymbolTable DeclaringTable { get; }
    
    /// <summary>
    /// Имя функции.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Список типов аргументов функции.
    /// </summary>
    public IReadOnlyList<ICompileTimeType> ArgumentTypes { get; }

    /// <summary>
    /// Получить информацию об объявлении функции.
    /// </summary>
    /// <returns></returns>
    public FunctionDefinitionInfo GetDefinitionInfo();
}