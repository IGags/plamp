using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class EmptyTypeInfo(TypedefNode typedefNode) : ITypeBuilderInfo
{
    private readonly int _arrayNesting;
    
    private readonly TypedefNode _typedefNode = typedefNode;

    private readonly List<IFieldBuilderInfo> _fields = [];
 
    private EmptyTypeInfo(TypedefNode typedefNode, int arrayNesting) : this(typedefNode)
    {
        _arrayNesting = arrayNesting;
    }
    
    public Type AsType()
    {
        Type type = _typedefNode.Type ?? throw new NullReferenceException();
        for (var i = 0; i < _arrayNesting; i++)
        {
            type = type.MakeArrayType();
        }

        return type;
    }

    public IReadOnlyList<IFieldInfo> Fields => _fields;

    public string Name => _typedefNode.Name.Value;

    public bool IsArrayType => _arrayNesting > 0;
    
    public TypedefNode Definition => _typedefNode;

    public IReadOnlyList<IFieldBuilderInfo> FieldBuilders => _fields;

    public void AddField(FieldDefNode defNode) => _fields.Add(new EmptyFieldInfo(defNode));

    public ITypeInfo MakeArrayType()
    {
        return new EmptyTypeInfo(_typedefNode, _arrayNesting + 1);
    }

    public ITypeInfo? ElementType()
    {
        return _arrayNesting == 0 ? null : new EmptyTypeInfo(_typedefNode, _arrayNesting - 1);
    }

    public bool Equals(ITypeInfo? other)
    {
        if (other is not EmptyTypeInfo typ) return false;
        return typ._typedefNode == _typedefNode && typ._arrayNesting == _arrayNesting;
    }
}