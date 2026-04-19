using System;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class BlankFieldInfo(ITypeInfo typeInfo, string name, ITypeInfo definingType) : IFieldBuilderInfo
{
    private readonly ITypeInfo _definingType = definingType;

    public FieldInfo AsField()
    {
        return Field ?? throw new NullReferenceException();
    }

    public ITypeInfo FieldType => typeInfo;
    
    public string Name => name;
    
    public FieldBuilder? Field { get; set; }

    public bool Equals(IFieldInfo? obj)
    {
        if (obj is not BlankFieldInfo emptyFld) return false;
        return Name.Equals(emptyFld.Name)
               && FieldType.Equals(emptyFld.FieldType)
               && _definingType.Equals(emptyFld._definingType);
    }
}