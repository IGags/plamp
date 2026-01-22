using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.Visitors.ModulePreCreation.FlowControlInsideLoop;

public class FlowControlInsideLoopContext : PreCreationContext
{
    public int LoopDepth { get; set; }
    
    public FlowControlInsideLoopContext(PreCreationContext other) : base(other)
    {
    }

    public FlowControlInsideLoopContext(ITranslationTable translationTable, List<ISymTable> dependencies) : base(translationTable, dependencies)
    {
    }
}