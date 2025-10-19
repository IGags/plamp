using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Abstractions.Ast.Node;

/// <summary>
/// Узел ast инициализации объекта пользовательского типа.
/// </summary>
/// <param name="type">Имя(ссылка на тип)</param>
/// <param name="fieldInitializers">Список выражений-инициализаторов полей</param>
public class InitTypeNode(TypeNode type, List<InitFieldNode> fieldInitializers) : NodeBase
{
    /// <summary>
    /// Имя(ссылка на тип)
    /// </summary>
    public TypeNode Type { get; private set; } = type;

    /// <summary>
    /// Список выражений-инициализаторов полей
    /// </summary>
    public IReadOnlyList<InitFieldNode> FieldInitializers => fieldInitializers;

    /// <inheritdoc/>
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Type;
        foreach (var initializer in FieldInitializers)
        {
            yield return initializer;
        }
    }

    /// <inheritdoc/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (child is TypeNode && Type == child && newChild is TypeNode newType) Type = newType;
        int ix;
        if (child is InitFieldNode init 
            && (ix = fieldInitializers.IndexOf(init)) != -1 
            && newChild is InitFieldNode newInit)
        {
            fieldInitializers[ix] = newInit;
        }
    }
}