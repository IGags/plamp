using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class GenericImplFieldInfo(ITypeInfo definitionType, IFieldInfo definitionField, ITypeInfo typeOverride) : IFieldInfo
{
    private readonly ITypeInfo _definitionType = definitionType;

    public FieldInfo AsField()
    {
        var type = _definitionType.AsType();
        var genericArgs = type.GetGenericArguments();

        FieldInfo? info;
        if (genericArgs.Any(x => x is GenericTypeParameterBuilder))
        {
            var fldInfo = definitionField.AsField();
            info = System.Reflection.Emit.TypeBuilder.GetField(type, fldInfo);
        }
        else
        {
            info = type.GetField(Name, BindingFlags.Public | BindingFlags.Instance);
        }
        
        if (info == null) throw new InvalidOperationException("Невозможно найти поле в объявляющем типе.");
        
        return info;
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