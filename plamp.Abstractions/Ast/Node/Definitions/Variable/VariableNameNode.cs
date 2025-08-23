using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Variable;

public class VariableNameNode(string value) : NodeBase
{
    public string Value { get; } = value;
    public override IEnumerable<NodeBase> Visit() => [];

    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}