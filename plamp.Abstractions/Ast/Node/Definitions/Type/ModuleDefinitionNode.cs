using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions.Type;

public class ModuleDefinitionNode(string moduleName) : NodeBase
{
    public string ModuleName { get; } = moduleName;

    public override IEnumerable<NodeBase> Visit() => [];

    public override void ReplaceChild(NodeBase child, NodeBase newChild) { }
}