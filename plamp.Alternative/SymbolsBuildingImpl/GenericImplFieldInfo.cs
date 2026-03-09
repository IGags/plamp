using System;
using System.Reflection;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class GenericImplFieldInfo(ITypeInfo definitionType, IFieldInfo underlyingBuilder, ITypeInfo typeOverride) : IFieldInfo
{
    public FieldInfo AsField()
    {
        var type = definitionType.AsType();
        var field = type.GetField(Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) throw new InvalidOperationException("Невозможно найти поле в объявляющем типе.");

        return field;
    }

    public ITypeInfo FieldType { get; } = typeOverride;

    public string Name => underlyingBuilder.Name;
    
    public FieldDefNode Definition => underlyingBuilder.Definition;
}