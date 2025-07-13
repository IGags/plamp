using plamp.Abstractions.Ast;
using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.ModulePreCreation;

public class PreCreationContext : BaseVisitorContext
{
    public PreCreationContext(BaseVisitorContext other) : base(other) { }

    public PreCreationContext(string fileName, ISymbolTable symbolTable) : base(fileName, symbolTable) { }
}