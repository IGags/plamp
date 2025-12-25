using System;
using System.Reflection;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class EmptyFieldInfo(FieldDefNode fieldDef) : IFieldInfo
{
    public FieldInfo AsField()
    {
        return fieldDef.Field ?? throw new NullReferenceException();
    }

    public ITypeInfo FieldType => fieldDef.FieldType.TypeInfo ?? throw new NullReferenceException();
    
    public string Name => fieldDef.Name.Value;
}