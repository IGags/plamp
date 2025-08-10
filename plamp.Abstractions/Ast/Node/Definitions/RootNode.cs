using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions;

public class RootNode(List<ImportNode> imports, ModuleDefinitionNode? moduleName, List<FuncNode> functions) : NodeBase
{
    private ModuleDefinitionNode? _moduleName = moduleName;
    public IReadOnlyList<ImportNode> Imports => imports;

    public ModuleDefinitionNode? ModuleName => _moduleName;

    public IReadOnlyList<FuncNode> Functions => functions;
    
    public override IEnumerable<NodeBase> Visit()
    {
        foreach (var import in imports)
        {
            yield return import;
        }

        if(_moduleName != null) yield return _moduleName;
        
        foreach (var func in functions)
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
        else if (newChild is FuncNode defChild
                 && child is FuncNode oldDef
                 && (ix = functions.IndexOf(oldDef)) != -1)
        {
            functions[ix] = defChild;
        }
    }
}