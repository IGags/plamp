using System.Collections.Generic;
using System.Reflection;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Validation.Models;

public record ValidationContext
{ 
    public ISymbolTable Table { get; init; }
    
    public NodeBase Ast { get; init; }

    public List<PlampException> Exceptions { get; init; } = [];
    
    public ICompiledAssemblyContainer AssemblyContainer { get; init; }
    
    public AssemblyName AssemblyName { get; init; }
    
    public string FileName { get; init; }
    
    public IReadOnlyList<NodeBase> CurrentCompilationSymbols { get; init; }
    
    public ValidationContext(ValidationContext other)
    {
        Table = other.Table;
        Ast = other.Ast;
        Exceptions = other.Exceptions;
        AssemblyContainer = other.AssemblyContainer;
        AssemblyName = other.AssemblyName;
        FileName = other.FileName;
        CurrentCompilationSymbols = other.CurrentCompilationSymbols;
    }
}