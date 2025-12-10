using System.Collections.Generic;
using plamp.Abstractions;
using plamp.Abstractions.Ast;
using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.SymbolTableBuilding;

public class SymbolTableBuildingContext : BaseVisitorContext
{
    public SymbolTable CurrentModuleTable { get; init; }
    
    public SymbolTableBuildingContext(
        SymbolTableBuildingContext other) : base(other)
    {
        CurrentModuleTable = other.CurrentModuleTable;
    }

    public SymbolTableBuildingContext(
        ITranslationTable translationTable, 
        List<ISymbolTable> dependencies, 
        SymbolTable table) : base(translationTable, dependencies)
    {
        CurrentModuleTable = table;
    }
}