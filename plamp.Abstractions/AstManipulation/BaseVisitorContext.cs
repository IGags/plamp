using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.AstManipulation;

public class BaseVisitorContext
{
    public required string FileName { get; init; }
    
    public string? ModuleName { get; set; }
    
    public required ISymbolTable SymbolTable { get; init; }

    public Dictionary<string, DefNode> Functions { get; init; } = [];

    public List<PlampException> Exceptions { get; init; } = [];
}