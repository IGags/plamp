using System.Collections.Generic;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.Ast.Node.Definitions.Type;

/// <summary>
/// Узел AST обозначающий использование того или иного типа(ссылка на тип)
/// </summary>
public class TypeNode : NodeBase
{
    private readonly List<TypeNode> _genericParameters = [];

    /// <summary>
    /// Узел AST обозначающий использование того или иного типа(ссылка на тип)
    /// </summary>
    /// <param name="typeName">Имя типа</param>
    /// <param name="genericParameters">Список дженерик параметров для типа</param>
    public TypeNode(TypeNameNode typeName, List<TypeNode>? genericParameters = null)
    {
        if(genericParameters != null) _genericParameters = genericParameters;
        TypeName = typeName;
    }

    /// <summary>
    /// Имя типа
    /// </summary>
    public TypeNameNode TypeName { get; private set; }

    /// <summary>
    /// Список дженерик параметров типа
    /// </summary>
    public IReadOnlyList<TypeNode> GenericParameters => _genericParameters;

    /// <summary>
    /// Список объявлений массива от внутреннего ко внешнему
    /// </summary>
    public List<ArrayTypeSpecificationNode> ArrayDefinitions { get; init; } = [];
    
    public ITypeInfo? TypeInfo { get; set; }

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return TypeName;
        foreach (var genericParameter in GenericParameters)
        {
            yield return genericParameter;
        }
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (TypeName == child && newChild is TypeNameNode newMember) TypeName = newMember;
        int ix;
        if (child is TypeNode genericParameter && (ix = _genericParameters.IndexOf(genericParameter)) != -1 && newChild is TypeNode newGeneric)
        {
            _genericParameters[ix] = newGeneric;
        }
    }
}