using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions;

public class TypeDefinitionNode(NodeBase name, List<NodeBase> members, List<NodeBase>? generics = null)
    : NodeBase
{
    private readonly List<NodeBase> _generics = generics ?? [];
    
    public NodeBase Name { get; private set; } = name;
    public IReadOnlyList<NodeBase> Members => members;
    public IReadOnlyList<NodeBase> Generics => _generics;

    public override IEnumerable<NodeBase> Visit()
    {
        yield return Name;
        
        foreach (var member in Members)
        {
            yield return member;
        }

        foreach (var generic in Generics)
        {
            yield return generic;
        }
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int childIndex;
        if (Name == child)
        {
            Name = newChild;
        }
        else if (-1 == (childIndex = members.IndexOf(child)))
        {
            members[childIndex] = newChild;
        }
        else if (-1 == (childIndex = _generics.IndexOf(child)))
        {
            _generics[childIndex] = newChild;
        }
    }
}