using System;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <inheritdoc/>
public class BlankFieldInfo : IFieldBuilderInfo
{
    private readonly ITypeInfo _typeInfo;
    private readonly string _name;
    private readonly ITypeInfo _definingType;
    
    private FieldInfo? _field;
    
    private FieldBuilder? _fieldBuilder;

    /// <summary>
    /// Создаёт описание поля строящегося типа.
    /// </summary>
    /// <param name="typeInfo">Тип поля. Не может быть void.</param>
    /// <param name="name">Имя поля. Не может быть пустым.</param>
    /// <param name="definingType">Тип, в котором объявлено поле.</param>
    /// <exception cref="InvalidOperationException">Имя поля пустое или тип поля void.</exception>
    public BlankFieldInfo(ITypeInfo typeInfo, string name, ITypeInfo definingType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Имя поля не может быть пустым.");
        if (SymbolSearchUtility.IsVoid(typeInfo))
            throw new InvalidOperationException("Поле не может иметь тип void.");

        _typeInfo = typeInfo;
        _name = name;
        _definingType = definingType;
    }

    /// <inheritdoc/>
    public FieldInfo AsField() => _fieldBuilder ?? _field ?? throw new NullReferenceException();

    /// <inheritdoc/>
    public ITypeInfo FieldType => _typeInfo;
    
    /// <inheritdoc/>
    public string Name => _name;

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

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not IFieldInfo info) return false;
        return Equals(info);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(_name, _definingType.GetHashCode(), _typeInfo.GetHashCode());
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
