using System;
using System.Collections.Generic;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Validators.BasicSemanticsValidators.TypeInference;

public class TypeInferenceInnerContext
{
    public required ISymbolTable SymbolTable { get; init; }

    public Dictionary<string, Type?> VariableTypeDict { get; init; } = [];
    
    public required IAssemblyContainer AssemblyContainer { get; init; }

    public List<PlampException> Exceptions { get; init; } = [];

    public Type? PrevInferredType { get; set; }
    
    public required IAssemblyContainer ThisModuleAssemblyContainer { get; init; }
    
    public required IReadOnlySet<string> ImportedModules { get; init; } = new HashSet<string>(); 
    
    public required string ModuleName { get; init; }
}