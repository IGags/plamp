using System.Collections.Generic;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Ast;

namespace plamp.Validators.BasicSemanticsValidators.TypeInference;

public record TypeInferenceContext
{
    public required ISymbolTable SymbolTable { get; init; }
    
    public required IAssemblyContainer AssemblyContainer { get; init; }
    
    public required IAssemblyContainer ThisModuleAssemblyContainer { get; init; }
    
    public required IReadOnlySet<string> ImportedModules { get; init; } = new HashSet<string>();

    public required string ModuleName { get; init; }
}