using System.Reflection;

namespace plamp.Abstractions;

/// <summary>
/// Информация об объявлении поля внутри типа.
/// </summary>
public class FieldDefinitionInfo
{
    /// <summary>
    /// Имя поля.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Тип поля.
    /// </summary>
    public required ICompileTimeType Type { get; init; }
    
    /// <summary>
    /// Представление поля внутри .net clr
    /// </summary>
    public FieldInfo? ClrField { get; private set; }

    /// <summary>
    /// Установить информацию о поле внутри .net clr
    /// </summary>
    /// <param name="clrField">Информация о поле из .net clr</param>
    public void SetClrField(FieldInfo clrField) => ClrField = clrField;
}