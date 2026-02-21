using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <inheritdoc/>
public class TypeBuilder(TypedefNode typedefNode) : ITypeBuilderInfo
{
    private readonly TypedefNode _typedefNode = typedefNode;

    private readonly List<IFieldBuilderInfo> _fields = [];

    private readonly List<GenericParameterBuilder> _genericParameterBuilders = [];

    public IReadOnlyList<GenericParameterBuilder> GenericParameterBuilders => _genericParameterBuilders;

    public IReadOnlyList<ITypeInfo> GenericParams => _genericParameterBuilders;
    
    public IReadOnlyList<IFieldInfo> Fields => _fields;

    public string Name => _typedefNode.Name.Value;

    public bool IsArrayType => false;

    public bool IsGenericTypeParameter => false;

    public bool IsGenericType => false;

    public bool IsGenericTypeDefinition => GenericParams.Count > 0;
    
    public System.Reflection.Emit.TypeBuilder? Type { get; set; }

    public TypedefNode Definition => _typedefNode;

    public IReadOnlyList<IFieldBuilderInfo> FieldBuilders => _fields;
    
    public IFnBuilderInfo? Constructor { get; set; }

    public void AddField(FieldDefNode defNode)
    {
        if (_fields.Any(x => x.Definition == defNode))
        {
            throw new InvalidOperationException("Type already has this field. If you see this, write to a compiler developer");
        }
        _fields.Add(new EmptyFieldInfo(defNode));
    }

    public ITypeInfo AddGenericParameter(GenericDefinitionNode genericParameter)
    {
        var genericParameterType = new GenericParameterBuilder(genericParameter);
        if (GenericParams.Any(x => x.Equals(genericParameterType))) throw new InvalidOperationException("Такой дженерик параметр уже объявлен в типе.");
        _genericParameterBuilders.Add(genericParameterType);
        return genericParameterType;
    }

    public ITypeInfo MakeArrayType() => new ArrayTypeBuilder(this);

    public ITypeInfo? ElementType() => null;

    public IReadOnlyList<ITypeInfo> GetGenericParameters() => GenericParams;

    public ITypeInfo? GetGenericTypeDefinition() => null;
    
    public Type AsType() => Type ?? throw new InvalidOperationException("Тип .net не может быть получен так как он не скомпилирован");

    public bool Equals(ITypeInfo? other)
    {
        if (other is not TypeBuilder otherType) return false;
        return otherType._typedefNode == _typedefNode;
    }
}