using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsImpl;

public class FldInfo(FieldInfo fld, string moduleName) : IFieldInfo
{
    private readonly FieldInfo _fld = fld;
    
    public FieldInfo AsField() => _fld;

    public ITypeInfo FieldType => TypeInfo.FromType(_fld.FieldType, moduleName);
    
    public string Name => _fld.Name;
    
    public bool Equals(IFieldInfo? other)
    {
        if (other is not FldInfo otherFld) return false;
        return _fld.Equals(otherFld._fld);
    }
}