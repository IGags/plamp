using System.Collections.Generic;
using plamp.Abstractions;
using plamp.Abstractions.Ast;
using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.ModulePreCreation;

public class PreCreationContext : BaseVisitorContext
{
    public PreCreationContext(PreCreationContext other) : base(other)
    {
    }

    public PreCreationContext(
        ITranslationTable translationTable, 
        List<ISymbolTable> dependencies) 
        : base(translationTable, dependencies)
    {
    }
}