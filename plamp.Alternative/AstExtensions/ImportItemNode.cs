using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Alternative.AstExtensions;

public class ImportItemNode(string name, string alias) : NodeBase
{
    public string Name { get; } = name;

    public string Alias { get; } = alias;
    
    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
    }
}