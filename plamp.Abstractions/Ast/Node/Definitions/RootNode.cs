using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions;

public class RootNode(List<ImportNode> imports, ModuleDefinitionNode? moduleName, List<DefNode> funcs) : NodeBase
{
    private ModuleDefinitionNode? _moduleName = moduleName;
    public IReadOnlyList<ImportNode> Imports => imports;

    public ModuleDefinitionNode? ModuleName => _moduleName;

    public IReadOnlyList<DefNode> Funcs => funcs;
    
    public override IEnumerable<NodeBase> Visit()
    {
        foreach (var import in imports)
        {
            yield return import;
        }

        if(_moduleName != null) yield return _moduleName;
        
        foreach (var func in funcs)
        {
            yield return func;
        }
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int ix;
        if (newChild is ImportNode importChild
            && child is ImportNode oldImport
            && (ix = imports.IndexOf(oldImport)) != -1)
        {
            imports[ix] = importChild;
        }
        else if (newChild is ModuleDefinitionNode moduleDefChild && moduleDefChild == ModuleName)
        {
            _moduleName = moduleDefChild;
        }
        else if (newChild is DefNode defChild
                 && child is DefNode oldDef
                 && (ix = funcs.IndexOf(oldDef)) != -1)
        {
            funcs[ix] = defChild;
        }
    }
}