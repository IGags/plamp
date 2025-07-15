using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions;

public class ImportItemNode(string name, string alias) : NodeBase
{
    public string Name { get; } = name;

    public string Alias { get; } = alias;
    
    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}