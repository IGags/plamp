using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <inheritdoc/>
public class TypeBuilder : ITypeBuilderInfo
{
    private readonly Dictionary<IFieldBuilderInfo, FieldDefNode> _fields = [];

    private readonly List<IGenericParameterBuilder> _genericParameterBuilders = [];

    private Type? _type;
    
    private System.Reflection.Emit.TypeBuilder? _typeBuilder;

    /// <summary>
    /// Создаёт описание обычного типа в контексте строящегося модуля.
    /// </summary>
    /// <param name="name">Имя типа. Не может быть пустым.</param>
    /// <param name="moduleName">Имя модуля, которому принадлежит тип.</param>
    /// <exception cref="InvalidOperationException">Имя типа пустое или имя модуля пустое.</exception>
    public TypeBuilder(string name, string moduleName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Имя типа не может быть пустым.");
        if (string.IsNullOrWhiteSpace(moduleName))
            throw new InvalidOperationException("Имя модуля не может быть пустым.");

        DefinitionName = name;
        ModuleName = moduleName;
    }

    /// <inheritdoc/>
    public string ModuleName { get; }
    
    /// <inheritdoc/>
    public string DefinitionName { get; }

    /// <inheritdoc/>
    public IReadOnlyList<IGenericParameterBuilder> GenericParameterBuilders => _genericParameterBuilders;
    
    /// <inheritdoc/>
    public IReadOnlyList<IFieldInfo> Fields => _fields.Keys.ToList();
    
    /// <inheritdoc/>
    public string Name
    {
        get
        {
            var defName = DefinitionName;
            if (_genericParameterBuilders.Count == 0) return defName;
            defName += "[" + string.Join(", ", _genericParameterBuilders.Select(x => x.Name)) + "]";
            return defName;
        }
    }

    /// <inheritdoc/>
    public bool IsArrayType => false;

    /// <inheritdoc/>
    public bool IsGenericTypeParameter => false;

    /// <inheritdoc/>
    public bool IsGenericType => false;

    /// <inheritdoc/>
    public bool IsGenericTypeDefinition => _genericParameterBuilders.Count > 0;
    
    /// <summary>
    /// Создаёт экземпляр текущего класса
    /// </summary>
    /// <param name="name">Имя типа</param>
    /// <param name="genericParameters">Список дженерик параметров типа, должен быть уникальным по имени. Пустой список - тип не объявление дженерика.</param>
    /// <param name="moduleName">Имя модуля, которому принадлежит тип</param>
    /// <exception cref="InvalidOperationException">Имя типа пустое, имя модуля пустое, или список generic-параметров некорректен.</exception>
    public TypeBuilder(string name, IReadOnlyList<IGenericParameterBuilder> genericParameters, string moduleName) 
        : this(name, moduleName)
    {
        foreach (var parameter in genericParameters)
        {
            AddGenericParameter(parameter);
        }
    }
    
    /// <summary>
    /// Добавление дженерик параметра в тип
    /// </summary>
    /// <param name="genericParameter">Описание параметра, должен быть уникальным по имени в типе и иметь схожий с типом объявляющий модуль</param>
    /// <exception cref="InvalidOperationException">Ошибка происходит, если у типа уже есть дженерик с таким именем, или если модуль дженерик параметра отличается от модуля типа.</exception>
    private void AddGenericParameter(IGenericParameterBuilder genericParameter)
    {
        ThrowIfComplete();
        if (!genericParameter.ModuleName.Equals(ModuleName)) throw new InvalidOperationException();
        if (!genericParameter.IsGenericTypeParameter) throw new InvalidOperationException();
        if (_genericParameterBuilders.Any(x => x.Equals(genericParameter))) throw new InvalidOperationException("Такой дженерик параметр уже объявлен в типе.");
        _genericParameterBuilders.Add(genericParameter);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
    
    /// <inheritdoc/>
    public IReadOnlyList<IFieldBuilderInfo> FieldBuilders => _fields.Keys.ToList();

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public ITypeInfo MakeArrayType()
    {
        if(_genericParameterBuilders.Count > 0) throw new InvalidOperationException("Невозможно сделать тип массива из объявления дженерик типа");
        return new ArrayTypeBuilder(this);
    }

    /// <inheritdoc/>
    public ITypeInfo? MakeGenericType(IReadOnlyList<ITypeInfo> genericTypeArguments)
    {
        if (_genericParameterBuilders.Count == 0) return null;
        return new GenericTypeBuilder(this, genericTypeArguments);
    }

    /// <inheritdoc/>
    public ITypeInfo? ElementType() => null;

    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericParameters() => _genericParameterBuilders;

    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericArguments() => [];


    /// <inheritdoc/>
    public ITypeInfo? GetGenericTypeDefinition() => null;
    
    /// <inheritdoc/>
    public Type AsType() => _typeBuilder ?? _type ?? throw new InvalidOperationException("Тип .net не может быть получен так как он не скомпилирован");
    
    /// <inheritdoc />
    public bool TryGetDefinition(IFieldBuilderInfo info, [NotNullWhen(true)] out FieldDefNode? defNode)
    {
        return _fields.TryGetValue(info, out defNode);
    }

    /// <inheritdoc/>
    public bool Equals(ITypeInfo? other)
    {
        //Нельзя использовать поиск по таблице символов, это повлечёт рекурсию на equality
        if (other is not TypeBuilder otherType) return false;
        if (!otherType.ModuleName.Equals(ModuleName)) return false;
        if(!otherType.Name.Equals(Name)) return false;
        return true;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var code = new HashCode();
        code.Add(GetType());
        code.Add(Name);
        code.Add(ModuleName);
        return code.ToHashCode();
    }
    
    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not ITypeInfo other) return false;
        return Equals(other);
    }

    /// <summary>
    /// Проверка на то, завершено ли создание типа, если да - то ошибка
    /// </summary>
    private void ThrowIfComplete()
    {
        if (_type != null) throw new InvalidOperationException("Создание типа завершено, дальнейшая модификация запрещена");
    }
}
