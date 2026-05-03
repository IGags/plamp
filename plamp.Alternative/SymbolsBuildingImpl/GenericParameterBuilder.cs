using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <summary>
/// Представление аргумента открытого дженерик типа, тоже является типом так как может служить типом полей типа-определения
/// Не появляется в таблице символов модуля, в котором объявлен дженерик тип
/// </summary>
public class GenericParameterBuilder(string name, string moduleName) : IGenericParameterBuilder
{
    private Type? _genericParameterType;
    private GenericTypeParameterBuilder? _parameterBuilder;

    /// <summary>
    /// Имя типа внутри дженерик объявления
    /// </summary>
    public string Name { get; } = name;

    public string ModuleName => moduleName;

    public string DefinitionName => Name;

    /// <inheritdoc/>
    public Type? GenericParameterType
    {
        get => _genericParameterType;
        set
        {
            ThrowIfComplete();
            _parameterBuilder = null;
            _genericParameterType = value;
        }
    }

    /// <inheritdoc/>
    public GenericTypeParameterBuilder? ParameterBuilder
    {
        get
        {
            ThrowIfComplete();
            return _parameterBuilder;
        }
        set
        {
            ThrowIfComplete();
            _parameterBuilder = value;
        }
    }

    /// <summary>
    /// У дженерик параметра не может быть полей
    /// </summary>
    public IReadOnlyList<IFieldInfo> Fields => [];
    
    /// <summary>
    /// Дженерик параметр внутри объявления типа не может никогда быть массивом
    /// </summary>
    public bool IsArrayType => false;
    
    /// <summary>
    /// Дженерик параметр не может быть закрытым дженериком
    /// </summary>
    public bool IsGenericType => false;
    
    /// <summary>
    /// Дженерик параметр не может быть объявлением дженерик типа 
    /// </summary>
    public bool IsGenericTypeDefinition => false;
    
    /// <summary>
    /// Прямое назначение типа - быть дженерик параметром
    /// </summary>
    public bool IsGenericTypeParameter => true;

    /// <inheritdoc cref="ITypeInfo.AsType"/>
    public Type AsType()
    {
        return _parameterBuilder ?? _genericParameterType ?? throw new InvalidOperationException("Тип .net не может быть получен так как он не скомпилирован");
    }

    /// <inheritdoc cref="ITypeInfo.MakeArrayType"/>
    public ITypeInfo MakeArrayType() => new ArrayTypeBuilder(this);

    public ITypeInfo? MakeGenericType(IReadOnlyList<ITypeInfo> genericTypeArguments) => null;

    /// <inheritdoc cref="ITypeInfo.ElementType"/>
    public ITypeInfo? ElementType() => null;

    /// <inheritdoc cref="ITypeInfo.GetGenericParameters" />
    public IReadOnlyList<ITypeInfo> GetGenericParameters() => [];

    public IReadOnlyList<ITypeInfo> GetGenericArguments() => [];

    /// <inheritdoc cref="ITypeInfo.GetGenericTypeDefinition"/>
    public ITypeInfo? GetGenericTypeDefinition() => null;
    
    public bool Equals(ITypeInfo? other)
    {
        if (other is not GenericParameterBuilder otherBuilder) return false;
        return Name == otherBuilder.Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), DefinitionName.GetHashCode(), ModuleName.GetHashCode(), Name.GetHashCode());
    }

    public void ThrowIfComplete()
    {
        if (_genericParameterType != null)
            throw new InvalidOperationException("Создание параметра завершено, дополнительная модификация запрещена.");
    }
}