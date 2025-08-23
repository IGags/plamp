using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Type;

public class TypeNode(TypeNameNode typeName, List<NodeBase>? innerGenerics = null) : NodeBase
{
    private readonly List<NodeBase> _innerGenerics = innerGenerics ?? [];
    
    public TypeNameNode TypeName { get; private set; } = typeName;
    
    public IReadOnlyList<NodeBase> InnerGenerics => _innerGenerics;

    public System.Type? Symbol { get; protected set; }

    public void SetType(System.Type type) => Symbol = type;

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
        if (TypeName == child && newChild is TypeNameNode newMember) TypeName = newMember;
        else if (-1 == (childIndex = _innerGenerics.IndexOf(child))) _innerGenerics[childIndex] = newChild;
    }
}