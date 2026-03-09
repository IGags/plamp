using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <summary>
/// Представление аргумента открытого дженерик типа, тоже является типом так как может служить типом полей типа-определения
/// Не появляется в таблице символов модуля, в котором объявлен дженерик тип
/// </summary>
public class GenericParameterBuilder(GenericDefinitionNode definitionNode) : ITypeInfo
{
    private readonly GenericDefinitionNode _definitionNode = definitionNode;

    /// <summary>
    /// Имя типа внутри дженерик объявления
    /// </summary>
    public string Name { get; } = definitionNode.Name.Value;

    /// <summary>
    /// Представление внутри системы типов .net, нужно для билдинга в полноценный тип.
    /// </summary>
    public GenericTypeParameterBuilder? TypeBuilder { get; set; }
    
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
        return TypeBuilder ?? throw new InvalidOperationException("Тип .net не может быть получен так как он не скомпилирован");
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
        return _definitionNode == otherBuilder._definitionNode;
    }
}