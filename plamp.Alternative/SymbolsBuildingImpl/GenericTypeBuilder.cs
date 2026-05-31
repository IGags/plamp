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

    /// <summary>
    /// Создать реализацию дженерик типа.
    /// </summary>
    /// <param name="definition">Тип объявления дженерик типа, обязан быть открытым дженериком</param>
    /// <param name="genericArguments">Список аргументов дженерик типа, ни один не должен быть объявлением дженерик типа.</param>
    /// <exception cref="InvalidOperationException">
    /// Происходит если число дженерик аргументов не равно числу дженерик параметров
    /// или если базовый тип не дженерик объявление или если хотя бы один из дженерик аргументов - объявление дженерик типа.
    /// </exception>
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

    /// <summary>
    /// Замещает параметры из объявления типа аргументами реализации в полях типа.
    /// </summary>
    /// <param name="definitionFields">Список полей типа-объявления</param>
    /// <param name="genericMapping">Список соответствий параметров из дженерик объявления аргументам из реализации.</param>
    /// <returns>Массив реализованных полей, число равно базовому типу</returns>
    private IFieldInfo[] OverrideGenericFields(IReadOnlyList<IFieldInfo> definitionFields, IReadOnlyDictionary<ITypeInfo, ITypeInfo> genericMapping)
    {
        var newFields = new List<IFieldInfo>();
        foreach (var field in definitionFields)
        {
            var implType = GenericImplementationHelper.ImplementType(field.FieldType, genericMapping);
            newFields.Add(new GenericImplFieldInfo(this, field, implType));
        }

        return newFields.ToArray();
    }
    
    /// <inheritdoc/>
    public IReadOnlyList<IFieldInfo> Fields => _fields;

    /// <inheritdoc/>
    public string ModuleName => _definition.ModuleName;

    /// <inheritdoc/>
    public string DefinitionName => _definition.DefinitionName;
    
    /// <inheritdoc/>
    public string Name
    {
        get
        {
            var argsNames = _genericArguments.Select(x => x.Name);
            return _definition.DefinitionName + $"[{string.Join(", ", argsNames)}]";
        }
    }

    /// <inheritdoc/>
    public bool IsArrayType => false;

    /// <inheritdoc/>
    public bool IsGenericType => true;

    /// <inheritdoc/>
    public bool IsGenericTypeDefinition => false;

    /// <inheritdoc/>
    public bool IsGenericTypeParameter => false;
    
    /// <inheritdoc/>
    public Type AsType()
    {
        var argTypes = _genericArguments.Select(x => x.AsType()).ToArray();
        return _definition.AsType().MakeGenericType(argTypes);
    }
    
    /// <inheritdoc/>
    public ITypeInfo MakeArrayType() => new ArrayTypeBuilder(this);
    
    /// <inheritdoc/>
    public ITypeInfo? MakeGenericType(IReadOnlyList<ITypeInfo> genericTypeArguments) => null;

    /// <inheritdoc/>
    public ITypeInfo? ElementType() => null;

    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericParameters() => [];

    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericArguments() => _genericArguments;

    /// <inheritdoc/>
    public ITypeInfo GetGenericTypeDefinition() => _definition;

    /// <inheritdoc/>
    public bool Equals(ITypeInfo? other)
    {
        if (other is not GenericTypeBuilder genericTypeBuilder) return false;
        
        return genericTypeBuilder._definition.Equals(_definition)
            && _genericArguments.SequenceEqual(genericTypeBuilder._genericArguments);
    }

    /// <inheritdoc/>
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
    
    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not ITypeInfo other) return false;
        return Equals(other);
    }
}