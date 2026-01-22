using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class EmptyTypeInfo(TypedefNode typedefNode) : ITypeBuilderInfo
{
    private readonly EmptyTypeInfo? _elementType;
    
    private readonly TypedefNode _typedefNode = typedefNode;

    private readonly List<IFieldBuilderInfo> _fields = [];
    
    private EmptyTypeInfo(TypedefNode typedefNode, EmptyTypeInfo elementType) : this(typedefNode)
    {
        _elementType = elementType;
    }
    
    public Type AsType()
    {
        Type type;
        if (_elementType != null)
        {
            type = _elementType.AsType();
            type = type.MakeArrayType();
        }
        else
        {
            type = _typedefNode.Type ?? throw new NullReferenceException();
        }

        return type;
    }

    public IReadOnlyList<IFieldInfo> Fields => _fields;

    public string Name => _typedefNode.Name.Value;

    public bool IsArrayType => _elementType != null;
    
    public TypedefNode Definition => _typedefNode;

    public IReadOnlyList<IFieldBuilderInfo> FieldBuilders => _fields;

    public void AddField(FieldDefNode defNode) => _fields.Add(new EmptyFieldInfo(defNode));

    public ITypeInfo MakeArrayType()
    {
        return new EmptyTypeInfo(_typedefNode, this);
    }

    public ITypeInfo? ElementType()
    {
        return _elementType;
    }

    public bool Equals(ITypeInfo? other)
    {
        if (other is not EmptyTypeInfo typ) return false;
        if (typ._typedefNode != _typedefNode) return false;
        if (_elementType == null && typ._elementType != null 
            || _elementType != null && typ._elementType == null) return false;

        if (_elementType == null && typ._elementType == null) return true;
        
        return _elementType!.Equals(typ._elementType);
    }
}