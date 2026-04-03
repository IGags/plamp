using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <inheritdoc/>
public class TypeBuilder(string name, SymTableBuilder definingTable) : ITypeBuilderInfo
{
    private readonly List<IFieldBuilderInfo> _fields = [];

    private readonly List<GenericParameterBuilder> _genericParameterBuilders = [];
    
    public string GenericDefinitionName => _genericParameterBuilders.Count != 0 ? name : throw new InvalidOperationException("Тип не является объявлением дженерик типа");

    public IReadOnlyList<IGenericParameterBuilder> GenericParameterBuilders => _genericParameterBuilders;

    public IReadOnlyList<ITypeInfo> GenericParams => _genericParameterBuilders;
    
    public IReadOnlyList<IFieldInfo> Fields => _fields;

    public string Name
    {
        get
        {
            var defName = name;
            if (_genericParameterBuilders.Any())
            {
                defName += "[" + string.Join(", ", _genericParameterBuilders.Select(x => x.Name)) + "]";
            }
            return defName;
        }
    }

    public bool IsArrayType => false;

    public bool IsGenericTypeParameter => false;

    public bool IsGenericType => false;

    public bool IsGenericTypeDefinition => GenericParams.Count > 0;
    
    public TypeBuilder(string name, IReadOnlyList<GenericDefinitionNode> genericParameters, SymTableBuilder definingTable) 
        : this(name, definingTable)
    {
        foreach (var parameter in genericParameters)
        {
            AddGenericParameter(parameter);
        }
    }
    
    private void AddGenericParameter(GenericDefinitionNode genericParameter)
    {
        var genericParameterType = new GenericParameterBuilder(genericParameter.Name.Value, this);
        if (GenericParams.Any(x => x.Equals(genericParameterType))) throw new InvalidOperationException("Такой дженерик параметр уже объявлен в типе.");
        _genericParameterBuilders.Add(genericParameterType);
    }

    public System.Reflection.Emit.TypeBuilder? Type { get; set; }

    public IReadOnlyList<IFieldBuilderInfo> FieldBuilders => _fields;

    public void AddField(FieldDefNode defNode)
    {
        var fieldType = defNode.FieldType.TypeInfo;
        if (fieldType == null) throw new InvalidOperationException("У поля нет корректного типа, ошибка компилятора");
        var newFld = new EmptyFieldInfo(fieldType, defNode.Name.Value, this);
        if (_fields.Any(x => x.Equals(newFld)))
        {
            throw new InvalidOperationException("Type already has this field. If you see this, write to a compiler developer");
        }
        
        definingTable.AddField(newFld, defNode);
        _fields.Add(newFld);
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
        if (!definingTable.TryGetDefinition(otherType, out var otherDef)) return false;
        if (!definingTable.TryGetDefinition(this, out var thisDef))
        {
            throw new InvalidOperationException("Тип не находится в модуле, который он считает объявляющим");
        }

        return otherDef == thisDef;
    }

    public override int GetHashCode()
    {
        var code = new HashCode();
        code.Add(Name);
        code.Add(definingTable.ModuleName);
        foreach (var field in _fields)
        {
            code.Add(field.Name);
        }
        //Чтобы не создавать циклической зависимости.
        code.Add(_genericParameterBuilders.Count);

        return code.ToHashCode();
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not ITypeInfo other) return false;
        return Equals(other);
    }
}