using System;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <inheritdoc/>
public class BlankFieldInfo(ITypeInfo typeInfo, string name, ITypeInfo definingType) : IFieldBuilderInfo
{
    private readonly ITypeInfo _definingType = definingType;
    
    private FieldInfo? _field;
    
    private FieldBuilder? _fieldBuilder;

    /// <inheritdoc/>
    public FieldInfo AsField() => _fieldBuilder ?? _field ?? throw new NullReferenceException();

    /// <inheritdoc/>
    public ITypeInfo FieldType => typeInfo;
    
    /// <inheritdoc/>
    public string Name => name;

    /// <inheritdoc/>
    public FieldInfo? Field
    {
        get => _field;
        set
        {
            ThrowIfComplete();
            _fieldBuilder = null;
            _field = value;
        }
    }

    /// <inheritdoc/>
    public FieldBuilder? Builder
    {
        get
        {
            ThrowIfComplete();
            return _fieldBuilder;
        }
        set
        {
            ThrowIfComplete();
            _fieldBuilder = value;
        }
    }

    /// <inheritdoc/>
    public bool Equals(IFieldInfo? obj)
    {
        if (obj is not BlankFieldInfo emptyFld) return false;
        return Name.Equals(emptyFld.Name)
               && FieldType.Equals(emptyFld.FieldType)
               && _definingType.Equals(emptyFld._definingType);
    }

    /// <summary>
    /// Бросить ошибку, если создание типа завершено
    /// </summary>
    private void ThrowIfComplete()
    {
        if (_field != null)
            throw new InvalidOperationException("Создание поля завершено, дальнейшая модификация запрещена");
    }
}