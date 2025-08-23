using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;

namespace plamp.Abstractions.AstManipulation;

public abstract class BaseVisitorContext(string fileName, ISymbolTable symbolTable)
{
    public string FileName { get; init; } = fileName;

    public string? ModuleName { get; set; }
    
    public ISymbolTable SymbolTable { get; init; } = symbolTable;

    public Dictionary<string, FuncNode> Functions { get; init; } = [];

    public List<PlampException> Exceptions { get; init; } = [];

    protected BaseVisitorContext(BaseVisitorContext other) : this(other.FileName, other.SymbolTable)
    {
        ModuleName = other.ModuleName;
        Exceptions = other.Exceptions;
        Functions = other.Functions;
    }
}