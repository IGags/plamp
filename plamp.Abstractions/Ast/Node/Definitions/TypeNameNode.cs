using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions;

public class TypeNameNode(string name) : NodeBase
{
    public string Name { get; } = name;

    public override IEnumerable<NodeBase> Visit() => [];

    public override void ReplaceChild(NodeBase child, NodeBase newChild) {}
}