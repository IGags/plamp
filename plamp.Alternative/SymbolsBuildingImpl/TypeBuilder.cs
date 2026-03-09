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
    
    public TypeBuilder(TypedefNode type, IReadOnlyList<GenericDefinitionNode> genericParameters) : this(type)
    {
        foreach (var parameter in genericParameters)
        {
            AddGenericParameter(parameter);
        }
    }
    
    private void AddGenericParameter(GenericDefinitionNode genericParameter)
    {
        var genericParameterType = new GenericParameterBuilder(genericParameter);
        if (GenericParams.Any(x => x.Equals(genericParameterType))) throw new InvalidOperationException("Такой дженерик параметр уже объявлен в типе.");
        _genericParameterBuilders.Add(genericParameterType);
    }

    public System.Reflection.Emit.TypeBuilder? Type { get; set; }

    public TypedefNode DefinitionNode => _typedefNode;

    public IReadOnlyList<IFieldBuilderInfo> FieldBuilders => _fields;

    public void AddField(FieldDefNode defNode)
    {
        if (_fields.Any(x => x.Definition == defNode))
        {
            throw new InvalidOperationException("Type already has this field. If you see this, write to a compiler developer");
        }
        _fields.Add(new EmptyFieldInfo(defNode));
    }

    public ITypeInfo MakeGenericType(ITypeInfo[] genericArguments)
    {
        if (GenericParams.Count == 0)
            throw new InvalidOperationException(
                "Нельзя использовать дженерик аргументы для типа, который не является объявлением дженерика");
        
        return new GenericTypeBuilder(this, genericArguments);
    }


    public ITypeInfo MakeArrayType()
    {
        if(GenericParams.Count > 0) throw new InvalidOperationException("Невозможно сделать тип массива из объявления дженерик типа");
        return new ArrayTypeBuilder(this);
    }

    public ITypeInfo? MakeGenericType(IReadOnlyList<ITypeInfo> genericTypeArguments)
    {
        if (_genericParameterBuilders.Count == 0) return null;
        return new GenericTypeBuilder(this, genericTypeArguments);
    }

    public ITypeInfo? ElementType() => null;

    public IReadOnlyList<ITypeInfo> GetGenericParameters() => GenericParams;

    public IReadOnlyList<ITypeInfo> GetGenericArguments() => [];


    public ITypeInfo? GetGenericTypeDefinition() => null;
    
    public Type AsType() => Type ?? throw new InvalidOperationException("Тип .net не может быть получен так как он не скомпилирован");

    public bool Equals(ITypeInfo? other)
    {
        if (other is not TypeBuilder otherType) return false;
        return otherType._typedefNode == _typedefNode;
    }
}