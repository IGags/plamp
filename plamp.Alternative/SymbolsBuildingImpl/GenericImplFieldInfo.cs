using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <summary>
/// Описание поля у имплементации дженерик типа.
/// Создаётся для всех полей у базового типа.
/// </summary>
public class GenericImplFieldInfo : IFieldInfo
{
    private readonly ITypeInfo _definitionType;
    private readonly IFieldInfo _definitionField;

    /// <summary>
    /// Построение объекта
    /// </summary>
    /// <param name="definitionType">Тип, в котором это поле содержится обязан быть имплементацией дженерика</param>
    /// <param name="definitionField">Поле, которое переопределяет текущий объект</param>
    /// <param name="typeOverride">Новый тип этого поля, если поле не дженерик, то тип должен сохранится от имплементации.</param>
    /// <exception cref="InvalidOperationException">Тип объявление не реализация дженерика, тип поля - объявление дженерик типа или void.</exception>
    public GenericImplFieldInfo(ITypeInfo definitionType, IFieldInfo definitionField, ITypeInfo typeOverride)
    {
        if (!definitionType.IsGenericType) throw new InvalidOperationException();
        if (typeOverride.IsGenericTypeDefinition) throw new InvalidOperationException();
        if (SymbolSearchUtility.IsVoid(typeOverride)) throw new InvalidOperationException("Поле не может иметь тип void.");
        
        _definitionField = definitionField;
        _definitionType = definitionType;
        FieldType = typeOverride;
    }
    
    /// <summary>
    /// Обратить этот объект в поле из рефлексии
    /// </summary>
    /// <returns>Поле из рефлексии .net</returns>
    /// <exception cref="InvalidOperationException">Тип ещё не скомпилирован или поле получить не удалось</exception>
    public FieldInfo AsField()
    {
        var type = _definitionType.AsType();
        var genericArgs = type.GetGenericArguments();

        FieldInfo? info;
        //Если реализация типа имеет хотя бы один System.Reflection.Emit.GenericTypeParameterBuilder то информация о поле получается по-другому из-за ограничений .net
        if (genericArgs.Any(x => x is GenericTypeParameterBuilder) 
            || type.GetGenericTypeDefinition() is System.Reflection.Emit.TypeBuilder)
        {
            var fldInfo = _definitionField.AsField();
            info = System.Reflection.Emit.TypeBuilder.GetField(type, fldInfo);
        }
        else
        {
            info = type.GetField(Name, BindingFlags.Public | BindingFlags.Instance);
        }
        
        if (info == null) throw new InvalidOperationException("Невозможно найти поле в объявляющем типе.");
        
        return info;
    }

    /// <inheritdoc/>
    public ITypeInfo FieldType { get; }

    /// <inheritdoc/>
    public string Name => _definitionField.Name;
    
    /// <inheritdoc/>
    public bool Equals(IFieldInfo? other)
    {
        if (other is not GenericImplFieldInfo otherFld) return false;
        return otherFld._definitionType.Equals(_definitionType)
               && otherFld.Name.Equals(Name)
               && otherFld.FieldType.Equals(FieldType);
    }
}
