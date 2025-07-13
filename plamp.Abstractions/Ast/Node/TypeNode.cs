using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class TypeNode(MemberNode typeName, List<NodeBase>? innerGenerics = null) : NodeBase
{
    private List<NodeBase> _innerGenerics = innerGenerics ?? [];
    public MemberNode TypeName { get; private set; } = typeName;
    public IReadOnlyList<NodeBase> InnerGenerics => _innerGenerics;

    public Type? Symbol { get; protected set; }

    public void SetType(Type type) => Symbol = type;

    public override IEnumerable<NodeBase> Visit()
    {
        yield return TypeName;
        foreach (var generic in InnerGenerics)
        {
            yield return generic;
        }
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int childIndex;
        if (TypeName == child && newChild is MemberNode newMember)
        {
            TypeName = newMember;
        }
        else if (-1 == (childIndex = _innerGenerics.IndexOf(child)))
        {
            _innerGenerics[childIndex] = newChild;
        }
    }
}