using System.Collections.Generic;

namespace plamp.Ast.Modules;

public interface IModuleDefinition
{
    public string Name { get; }
    
    public IReadOnlyList<ITypeDefinition> Types { get; }

    public IReadOnlyList<IModuleDefinition> Dependencies { get; }

    public ITypeDefinition FindType(string name);
    
    public IModuleDefinition FindDependency(string name);
    
    public ISymbolTable GetSymbolTable();
}