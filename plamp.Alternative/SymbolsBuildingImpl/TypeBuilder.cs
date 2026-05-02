using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <inheritdoc/>
public class TypeBuilder(string name, string moduleName) : ITypeBuilderInfo
{
    private readonly Dictionary<IFieldBuilderInfo, FieldDefNode> _fields = [];

    private readonly List<IGenericParameterBuilder> _genericParameterBuilders = [];

    private Type? _type;
    
    private System.Reflection.Emit.TypeBuilder? _typeBuilder;

    public string ModuleName => moduleName;
    
    public string DefinitionName => _genericParameterBuilders.Count == 0 ? name : $"{name}`{_genericParameterBuilders.Count}";

    public IReadOnlyList<IGenericParameterBuilder> GenericParameterBuilders => _genericParameterBuilders;

    public IReadOnlyList<ITypeInfo> GenericParams => _genericParameterBuilders;
    
    public IReadOnlyList<IFieldInfo> Fields => _fields.Keys.ToList();
    
    public string Name
    {
        get
        {
            var defName = name;
            if (_genericParameterBuilders.Count == 0) return defName;
            
            var ixSep = defName.LastIndexOf('`');
            ixSep = ixSep == -1 ? defName.Length : ixSep;
            defName = defName[..ixSep];
            defName += "[" + string.Join(", ", _genericParameterBuilders.Select(x => x.Name)) + "]";
            return defName;
        }
    }

    public bool IsArrayType => false;

    public bool IsGenericTypeParameter => false;

    public bool IsGenericType => false;

    public bool IsGenericTypeDefinition => GenericParams.Count > 0;
    
    public TypeBuilder(string name, IReadOnlyList<IGenericParameterBuilder> genericParameters, string moduleName) 
        : this(name, moduleName)
    {
        foreach (var parameter in genericParameters)
        {
            AddGenericParameter(parameter);
        }
    }
    
    private void AddGenericParameter(IGenericParameterBuilder genericParameter)
    {
        ThrowIfComplete();
        if (!genericParameter.ModuleName.Equals(ModuleName)) throw new InvalidOperationException();
        if (!genericParameter.IsGenericTypeParameter) throw new InvalidOperationException();
        if (GenericParams.Any(x => x.Equals(genericParameter))) throw new InvalidOperationException("Такой дженерик параметр уже объявлен в типе.");
        _genericParameterBuilders.Add(genericParameter);
    }

    public Type? Type
    {
        get => _type;
        set
        {
            ThrowIfComplete();
            _typeBuilder = null;
            _type = value;
        }
    }

    public System.Reflection.Emit.TypeBuilder? Builder
    {
        get
        {
            ThrowIfComplete();
            return _typeBuilder;
        }
        set
        {
            ThrowIfComplete();
            _typeBuilder = value;
        }
    }


    public IReadOnlyList<IFieldBuilderInfo> FieldBuilders => _fields.Keys.ToList();

    public void AddField(FieldDefNode defNode)
    {
        ThrowIfComplete();
        var fieldType = defNode.FieldType.TypeInfo;
        if (fieldType == null) throw new InvalidOperationException("У поля нет корректного типа, ошибка компилятора");
        var newFld = new BlankFieldInfo(fieldType, defNode.Name.Value, this);
        if (_fields.Any(x => x.Key.Name.Equals(newFld.Name)))
        {
            throw new InvalidOperationException("Type already has this field. If you see this, write to a compiler developer");
        }
        
        _fields.Add(newFld, defNode);
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
    
    public Type AsType() => _typeBuilder ?? _type ?? throw new InvalidOperationException("Тип .net не может быть получен так как он не скомпилирован");
    
    /// <inheritdoc />
    public bool TryGetDefinition(IFieldBuilderInfo info, [NotNullWhen(true)] out FieldDefNode? defNode)
    {
        return _fields.TryGetValue(info, out defNode);
    }

    public bool Equals(ITypeInfo? other)
    {
        //Нельзя использовать поиск по таблице символов, это повлечёт рекурсию на equality
        if (other is not TypeBuilder otherType) return false;
        if (!otherType.ModuleName.Equals(ModuleName)) return false;
        if(!otherType.Name.Equals(Name)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        var code = new HashCode();
        code.Add(GetType());
        code.Add(Name);
        code.Add(ModuleName);
        return code.ToHashCode();
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not ITypeInfo other) return false;
        return Equals(other);
    }

    private void ThrowIfComplete()
    {
        if (_type != null) throw new InvalidOperationException("Создание типа завершено, дальнейшая модификация запрещена");
    }
}