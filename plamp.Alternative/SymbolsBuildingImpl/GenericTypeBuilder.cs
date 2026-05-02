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
        var genericMapping = definition.GetGenericParameters()
            .Zip(genericArguments)
            .ToDictionary(x => x.First, y => y.Second);
        
        _fields = OverrideGenericFields(definitionFields, genericMapping);
    }

    private IFieldInfo[] OverrideGenericFields(IReadOnlyList<IFieldInfo> definitionFields, IReadOnlyDictionary<ITypeInfo, ITypeInfo> genericMapping)
    {
        var newFields = new List<IFieldInfo>();
        foreach (var field in definitionFields)
        {
            var implType = ImplementType(field.FieldType, genericMapping);
            newFields.Add(new GenericImplFieldInfo(this, field, implType));
        }

        return newFields.ToArray();
    }
    
    public static ITypeInfo ImplementType(
        ITypeInfo openType,
        IReadOnlyDictionary<ITypeInfo, ITypeInfo> typeMapping)
    {
        if (openType.IsGenericTypeDefinition)
            throw new InvalidOperationException("Нельзя сделать имплементацию для дженерик объявления");

        if (openType.IsGenericTypeParameter)
        {
            return typeMapping.GetValueOrDefault(openType) ??
                   throw new InvalidOperationException("Неполный маппинг типов для имплементации дженериков");
        }

        if (openType.IsArrayType)
        {
            var elemType = openType.ElementType();
            ArgumentNullException.ThrowIfNull(elemType);
            var elemImpl = ImplementType(elemType, typeMapping);
            return elemImpl.MakeArrayType();
        }

        if (openType.IsGenericType)
        {
            var openTypeDef = openType.GetGenericTypeDefinition();
            ArgumentNullException.ThrowIfNull(openTypeDef);
            var openTypeArgs = openType.GetGenericArguments();
            var implArgs = new List<ITypeInfo>();
            foreach (var argType in openTypeArgs)
            {
                implArgs.Add(ImplementType(argType, typeMapping));
            }

            var implType = openTypeDef.MakeGenericType(implArgs);
            ArgumentNullException.ThrowIfNull(implType);
            return implType;
        }

        return openType;
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