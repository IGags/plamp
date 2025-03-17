using System.Collections.Generic;
using plamp.Ast;
using plamp.Ast.Modules;

namespace plamp.Assembly.Implementations.Static;

public class StaticModuleDefinition : IModuleDefinition
{
    public string Name { get; }
    
    public IReadOnlyList<ITypeDefinition> Types { get; }
    
    public IReadOnlyList<IModuleDefinition> Dependencies { get; }
    
    public ITypeDefinition FindType(string name)
    {
        throw new System.NotImplementedException();
    }

    public IModuleDefinition FindDependency(string name)
    {
        throw new System.NotImplementedException();
    }

    public bool TryGetEntryPointMethod(out IMethodDefinition methodDefinition)
    {
        throw new System.NotImplementedException();
    }
}