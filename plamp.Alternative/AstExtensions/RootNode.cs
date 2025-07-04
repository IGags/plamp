using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Alternative.AstExtensions;

public class RootNode(List<ImportNode> imports, ModuleDefinitionNode moduleName, List<DefNode> funcs) : NodeBase
{
    public IReadOnlyList<ImportNode> Imports { get; } = imports;

    public ModuleDefinitionNode ModuleName { get; } = moduleName;

    public IReadOnlyList<DefNode> Funcs { get; } = funcs;
    
    public override IEnumerable<NodeBase> Visit()
    {
        foreach (var import in imports)
        {
            yield return import;
        }

        yield return moduleName;
        
        foreach (var func in funcs)
        {
            yield return func;
        }
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        throw new System.NotImplementedException();
    }
}