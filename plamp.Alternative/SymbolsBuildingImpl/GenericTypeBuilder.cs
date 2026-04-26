using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <summary>
/// Тип закрытого дженерика - все параметры типизированы чем-то.
/// </summary>
public class GenericTypeBuilder : ITypeInfo
{
    private readonly ITypeInfo _definition;
    private readonly IReadOnlyList<ITypeInfo> _genericArguments;
    private readonly IFieldInfo[] _fields;

    public GenericTypeBuilder(ITypeInfo definition, IReadOnlyList<ITypeInfo> genericArguments)
    {
        if (!definition.IsGenericTypeDefinition)
            throw new InvalidOperationException("У закрытого дженерик типа должно быть дженерик объявление, от которого он строится");

        if (genericArguments.Any(x => x.IsGenericTypeDefinition))
            throw new InvalidOperationException("Дженерик тип не может иметь объявление дженерик типа в качестве своего аргумента");
        
        if (definition.GetGenericParameters().Count != genericArguments.Count)
            throw new InvalidOperationException("Число дженерик аргументов у закрытого дженерика должно соответствовать числу параметров у объявления дженерик типа");
        
        _definition = definition;
        _genericArguments = genericArguments;

        var definitionFields = _definition.Fields;
        var definitionParameters = definition.GetGenericParameters();
        _fields = OverrideGenericFields(definitionFields, definitionParameters);
    }

    private IFieldInfo[] OverrideGenericFields(IReadOnlyList<IFieldInfo> definitionFields, IReadOnlyList<ITypeInfo> genericArguments)
    {
        var parameterIx = 0;
        var fields = new List<IFieldInfo>();
        foreach (var fieldInfo in definitionFields)
        {
            var fldType = fieldInfo.FieldType;
            if (fldType.IsGenericTypeParameter && genericArguments.Contains(fldType))
            {
                fields.Add(new GenericImplFieldInfo(this, fieldInfo, _genericArguments[parameterIx++]));
            }
            else
            {
                fields.Add(fieldInfo);
            }
        }
        
        return fields.ToArray();
    }

    public IReadOnlyList<IFieldInfo> Fields => _fields;

    public string ModuleName => _definition.ModuleName;
    public string DefinitionName => _definition.DefinitionName;
    
    public string Name
    {
        get
        {
            var argsNames = _genericArguments.Select(x => x.Name);
            var ixSep = _definition.DefinitionName.LastIndexOf('`');
            ixSep = ixSep == -1 ? _definition.DefinitionName.Length : ixSep;
            var name = _definition.DefinitionName[..ixSep];
            return name + $"[{string.Join(", ", argsNames)}]";
        }
    }

    public bool IsArrayType => false;

    public bool IsGenericType => true;

    public bool IsGenericTypeDefinition => false;

    public bool IsGenericTypeParameter => false;
    
    public Type AsType()
    {
        var argTypes = _genericArguments.Select(x => x.AsType()).ToArray();
        return _definition.AsType().MakeGenericType(argTypes);
    }
    
    public ITypeInfo MakeArrayType() => new ArrayTypeBuilder(this);
    public ITypeInfo? MakeGenericType(IReadOnlyList<ITypeInfo> genericTypeArguments) => null;

    public ITypeInfo? ElementType() => null;

    public IReadOnlyList<ITypeInfo> GetGenericParameters() => [];

    public IReadOnlyList<ITypeInfo> GetGenericArguments() => _genericArguments;

    public ITypeInfo GetGenericTypeDefinition() => _definition;

    public bool Equals(ITypeInfo? other)
    {
        if (other is not GenericTypeBuilder genericTypeBuilder) return false;
        
        return genericTypeBuilder._definition.Equals(_definition)
            && _genericArguments.SequenceEqual(genericTypeBuilder._genericArguments);
    }

    public override int GetHashCode()
    {
        var code = new HashCode();
        code.Add(_definition.GetHashCode());
        foreach (var arg in _genericArguments)
        {
            code.Add(arg.GetHashCode());
        }

        return code.ToHashCode();
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not ITypeInfo other) return false;
        return Equals(other);
    }
}