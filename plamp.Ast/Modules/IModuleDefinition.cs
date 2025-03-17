using System.Collections.Generic;

namespace plamp.Ast.Modules;

public interface IModuleDefinition
{
    string Name { get; }
    
    IReadOnlyList<ITypeDefinition> Types { get; }

    IReadOnlyList<IModuleDefinition> Dependencies { get; }

    ITypeDefinition FindType(string name);
    
    IModuleDefinition FindDependency(string name);
    
    bool TryGetEntryPointMethod(out IMethodDefinition methodDefinition);
}