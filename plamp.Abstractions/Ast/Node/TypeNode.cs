using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class TypeNode : NodeBase
{
    private readonly List<NodeBase> _innerGenerics;
    public NodeBase TypeName { get; private set; }
    public IReadOnlyList<NodeBase> InnerGenerics => _innerGenerics;

    public virtual Type Symbol { get; init; } = null;

    public TypeNode(NodeBase typeName, List<NodeBase> innerGenerics)
    {
        TypeName = typeName;
        _innerGenerics = innerGenerics;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        yield return TypeName;
        if(InnerGenerics == null) yield break;
        foreach (var generic in InnerGenerics)
        {
            yield return generic;
        }
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int childIndex;
        if (TypeName == child)
        {
            TypeName = newChild;
        }
        else if (-1 == (childIndex = _innerGenerics.IndexOf(child)))
        {
            _innerGenerics[childIndex] = newChild;
        }
    }
}