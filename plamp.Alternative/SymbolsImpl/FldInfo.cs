using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsImpl;

public class FldInfo(FieldInfo fld) : IFieldInfo
{
    public FieldInfo AsField() => fld;

    public ITypeInfo FieldType => new TypeInfo(fld.FieldType);
    
    public string Name => fld.Name;
}