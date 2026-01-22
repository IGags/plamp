using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.AstManipulation;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.Visitors.ModulePreCreation;

public class PreCreationContext : BaseVisitorContext
{
    public PreCreationContext(PreCreationContext other) : base(other)
    {
    }

    public PreCreationContext(
        ITranslationTable translationTable, 
        List<ISymTable> dependencies) 
        : base(translationTable, dependencies)
    {
    }
}