using System;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class EmptyFieldInfo(FieldDefNode fieldDef) : IFieldBuilderInfo
{
    public FieldInfo AsField()
    {
        return Field ?? throw new NullReferenceException();
    }

    public ITypeInfo FieldType => fieldDef.FieldType.TypeInfo ?? throw new NullReferenceException();
    
    public string Name => fieldDef.Name.Value;

    public FieldDefNode Definition => fieldDef;
    
    public FieldBuilder? Field { get; set; }
}