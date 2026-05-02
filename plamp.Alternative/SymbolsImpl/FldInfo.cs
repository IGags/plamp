using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsImpl;

/// <inheritdoc/>
public class FldInfo(FieldInfo fld, string moduleName) : IFieldInfo
{
    private readonly FieldInfo _fld = fld;

    /// <inheritdoc/>
    public FieldInfo AsField() => _fld;
    
    /// <inheritdoc/>
    public ITypeInfo FieldType => TypeInfo.FromType(_fld.FieldType, moduleName);
    
    /// <inheritdoc/>
    public string Name => _fld.Name;
    
    /// <inheritdoc/>
    public bool Equals(IFieldInfo? other)
    {
        if (other is not FldInfo otherFld) return false;
        return _fld.Equals(otherFld._fld);
    }
}