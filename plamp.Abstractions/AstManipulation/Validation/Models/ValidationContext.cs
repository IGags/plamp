using System.Collections.Generic;
using System.Reflection;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.AstManipulation.Validation.Models;

public record ValidationContext
{ 
    public ISymbolTable Table { get; init; }

    public List<PlampException> Exceptions { get; init; } = [];
    
    public ICompiledAssemblyContainer AssemblyContainer { get; init; }
    
    public AssemblyName AssemblyName { get; init; }
    
    public string FileName { get; init; }
    
    public IReadOnlyList<NodeBase> CurrentCompilationSymbols { get; init; }
    
    public ValidationContext(ValidationContext other)
    {
        Table = other.Table;
        Exceptions = other.Exceptions;
        AssemblyContainer = other.AssemblyContainer;
        AssemblyName = other.AssemblyName;
        FileName = other.FileName;
        CurrentCompilationSymbols = other.CurrentCompilationSymbols;
    }
}