using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions;
using plamp.Abstractions.Ast;
using plamp.Abstractions.AstManipulation;
using plamp.Intrinsics;

namespace plamp.Alternative.Visitors.ModulePreCreation;

public class PreCreationContext : BaseVisitorContext
{
    public SymbolTable SymbolTable { get; init; }

    public PreCreationContext(PreCreationContext other) : base(other)
    {
        SymbolTable = other.SymbolTable;
        Dependencies = [RuntimeSymbols.GetSymbolTable];
    }

    public IEnumerable<ISymbolTable> GetAllSymbols() => Dependencies.Concat([SymbolTable]);

    public PreCreationContext(ITranslationTable translationTable, SymbolTable symbolTable) : base(translationTable)
    {
        SymbolTable = symbolTable;
    }
}