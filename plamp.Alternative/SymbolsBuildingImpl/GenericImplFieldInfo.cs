using System;
using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class GenericImplFieldInfo(ITypeInfo definitionType, IFieldInfo definitionField, ITypeInfo typeOverride) : IFieldInfo
{
    private readonly ITypeInfo _definitionType = definitionType;

    public FieldInfo AsField()
    {
        var type = _definitionType.AsType();
        var field = type.GetField(Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) throw new InvalidOperationException("Невозможно найти поле в объявляющем типе.");

        return field;
    }

    public ITypeInfo FieldType { get; } = typeOverride;

    public string Name => definitionField.Name;
    
    public bool Equals(IFieldInfo? other)
    {
        if (other is not GenericImplFieldInfo otherFld) return false;
        return otherFld._definitionType.Equals(_definitionType)
               && otherFld.Name.Equals(Name)
               && otherFld.FieldType.Equals(FieldType);
    }
}