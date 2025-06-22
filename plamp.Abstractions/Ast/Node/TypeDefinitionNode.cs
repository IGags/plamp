using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class TypeDefinitionNode : NodeBase
{
    private readonly List<NodeBase> _members;

    private readonly List<NodeBase> _generics;
    
    public NodeBase Name { get; private set; }
    public IReadOnlyList<NodeBase> Members => _members;
    public IReadOnlyList<NodeBase> Generics => _generics;

    public TypeDefinitionNode(NodeBase name, List<NodeBase> members,  List<NodeBase> generics = null)
    {
        Name = name;
        _members = members;
        _generics = generics;
    }
    
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Name;
        
        foreach (var member in Members)
        {
            yield return member;
        }
        
        if(Generics == null) yield break;

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
        else if (-1 == (childIndex = _members.IndexOf(child)))
        {
            _members[childIndex] = newChild;
        }
        else if (-1 == (childIndex = _generics.IndexOf(child)))
        {
            _generics[childIndex] = newChild;
        }
    }
}