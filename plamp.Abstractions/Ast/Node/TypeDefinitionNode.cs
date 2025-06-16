using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node;

public class TypeDefinitionNode : NodeBase
{
    public NodeBase Name { get; }
    public List<NodeBase> Members { get; }
    public List<NodeBase> Generics { get; }

    public TypeDefinitionNode(NodeBase name, List<NodeBase> members,  List<NodeBase> generics = null)
    {
        Name = name;
        Members = members;
        Generics = generics;
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
}