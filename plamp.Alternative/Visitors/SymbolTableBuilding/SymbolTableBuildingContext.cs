using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.AstManipulation;
using plamp.Abstractions.Symbols;

namespace plamp.Alternative.Visitors.SymbolTableBuilding;

public class SymbolTableBuildingContext : BaseVisitorContext
{
    public ISymTableBuilder SymTableBuilder { get; init; }
    
    public SymbolTableBuildingContext(
        SymbolTableBuildingContext other) : base(other)
    {
        SymTableBuilder = other.SymTableBuilder;
    }

    public SymbolTableBuildingContext(
        ITranslationTable translationTable, 
        List<ISymTable> dependencies, 
        ISymTableBuilder symTableBuilder) : base(translationTable, dependencies)
    {
        SymTableBuilder = symTableBuilder;
    }
}