using System;
using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Symbols.SymTable;

/// <summary>
/// Информация об объявлении функции в рамках модуля.
/// </summary>
public interface IFnInfo : IModuleMember, IEquatable<IFnInfo>
{
    /// <summary>
    /// Имя функции, должно быть уникально среди всех членов в рамках модуля. Должно возвращать дженерик параметры.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Имя с которым объявлена функция без дженериков и прочего.
    /// </summary>
    public string DefinitionName { get; }
    
    /// <summary>
    /// Список аргументов, с которыми объявлена функция
    /// </summary>
    public IReadOnlyList<IArgInfo> Arguments { get; }
    
    /// <summary>
    /// Возвращаемый тип функции
    /// </summary>
    public ITypeInfo ReturnType { get; }
    
    /// <summary>
    /// Является ли данная функция дженерик-объявлением
    /// </summary>
    public bool IsGenericFuncDefinition { get; }
    
    /// <summary>
    /// Является ли данная функция дженерик-имплементацией
    /// </summary>
    public bool IsGenericFunc { get; }

    /// <summary>
    /// Параметры дженерик функции. [] если функция не несёт дженерики или является имплементацией дженерик функции.
    /// </summary>
    public IReadOnlyList<ITypeInfo> GetGenericParameters();

    /// <summary>
    /// Аргументы имплементированной дженерик функции. [] если функция не дженерик-реализация или не имеет дженерик аргументов. 
    /// </summary>
    public IReadOnlyList<ITypeInfo> GetGenericArguments();

    /// <summary>
    /// Поучить объявление дженерик функции по её реализации.
    /// </summary>
    /// <returns>Объявление дженерик функции или null, если функция не является дженерик реализацией чего-либо.</returns>
    public IFnInfo? GetGenericFuncDefinition();

    /// <summary>
    /// Создать на основе объявления дженерик функции её реализацию.
    /// Работает только в случае, если текущая функция - объявление дженерик функции. 
    /// </summary>
    /// <param name="genericTypeArguments">Список аргументов для дженерик функции</param>
    /// <exception cref="InvalidOperationException">Число дженерик аргументов не совпадает или некоторые из них - объявления дженерик типов.</exception>
    /// <returns>Новая функция или null, если исходная функция не имела открытое дженерик объявление</returns>
    public IFnInfo? MakeGenericFunc(IReadOnlyList<ITypeInfo> genericTypeArguments);
    
    /// <summary>
    /// Превратить эту функцию в метод .net runtime.
    /// Прямого перевода в метод не существует так как во время компиляции модуля функции в .net clr ещё может не существовать.
    /// </summary>
    /// <returns>Метод в .net или ошибка, если метода ещё не существует</returns>
    /// <exception cref="InvalidOperationException">Функция пока не скомпилирована.</exception>
    public MethodInfo AsFunc();
}