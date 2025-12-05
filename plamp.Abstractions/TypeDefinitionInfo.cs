using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast;

namespace plamp.Abstractions;

/// <summary>
/// Информация об объявлении типа внутри модуля
/// </summary>
public class TypeDefinitionInfo
{
    /// <summary>
    /// Имя типа
    /// </summary>
    public required string TypeName { get; init; }
    
    /// <summary>
    /// Представление типа в .net clr
    /// </summary>
    public Type? ClrType { get; private set; }
    
    /// <summary>
    /// Позиция объявления типа в кодовом файле
    /// </summary>
    public FilePosition DefinitionPosition { get; init; }
    
    /// <summary>
    /// Поля, которые объявлены в типе
    /// </summary>
    public required List<FieldDefinitionInfo> Fields { get; init; }
    
    /// <summary>
    /// Если данный тип является массивом, то здесь лежит значение типа элемента массива != default.
    /// </summary>
    public ICompileTimeType? ArrayUnderlyingType { get; init; }

    /// <summary>
    /// Установить скомпилированный тип .net clr
    /// </summary>
    /// <param name="clrType">Тип внутри .net</param>
    public void SetClrType(Type clrType) => ClrType = clrType;
}