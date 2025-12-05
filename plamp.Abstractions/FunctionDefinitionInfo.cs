using System.Collections.Generic;
using System.Reflection;
using plamp.Abstractions.Ast;

namespace plamp.Abstractions;

/// <summary>
/// Информация об объявлении функции из .net clr
/// </summary>
public class FunctionDefinitionInfo
{
    /// <summary>
    /// Имя функции
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Тип возвращаемого значения
    /// </summary>
    public required ICompileTimeType ReturnType { get; init; }
    
    /// <summary>
    /// Список аргументов функции
    /// </summary>
    public required List<ICompileTimeType> ArgumentList { get; init; }
    
    /// <summary>
    /// Позиция объявления функции в кодовом файле
    /// </summary>
    public FilePosition DefinitionPosition { get; init; }
    
    /// <summary>
    /// Информация об объявлении типа внутри .net clr
    /// </summary>
    public MethodInfo? ClrMethod { get; private set; }

    /// <summary>
    /// Установить информацию об объявлении метода внутри .net clr
    /// </summary>
    /// <param name="clrMethod">Информация об объявлении метода внутри .net clr</param>
    public void SetClrMethod(MethodInfo clrMethod) => ClrMethod = clrMethod;
}