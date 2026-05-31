using System;
using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsImpl;

/// <inheritdoc/>
public class FldInfo : IFieldInfo
{
    private readonly FieldInfo _fld;
    private readonly string _moduleName;

    /// <summary>
    /// Создаёт описание runtime-поля.
    /// </summary>
    /// <param name="fld">Поле .NET runtime.</param>
    /// <param name="moduleName">Имя модуля. Не может быть пустым.</param>
    /// <exception cref="InvalidOperationException">Имя модуля пустое.</exception>
    public FldInfo(FieldInfo fld, string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            throw new InvalidOperationException("Имя модуля не может быть пустым.");

        _fld = fld;
        _moduleName = moduleName;
    }

    /// <inheritdoc/>
    public FieldInfo AsField() => _fld;
    
    /// <inheritdoc/>
    public ITypeInfo FieldType => TypeInfo.FromType(_fld.FieldType, _moduleName);
    
    /// <inheritdoc/>
    public string Name => _fld.Name;
    
    /// <inheritdoc/>
    public bool Equals(IFieldInfo? other)
    {
        if (other is not FldInfo otherFld) return false;
        return _fld.Equals(otherFld._fld);
    }
}
