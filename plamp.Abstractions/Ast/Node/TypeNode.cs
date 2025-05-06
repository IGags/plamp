using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class TypeNode : NodeBase
{
    public NodeBase TypeName { get; }
    public List<NodeBase> InnerGenerics { get; }

    public virtual Type Symbol { get; } = null;

    public TypeNode(NodeBase typeName, List<NodeBase> innerGenerics)
    {
        TypeName = typeName;
        InnerGenerics = innerGenerics;
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
}