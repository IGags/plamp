using System.Collections.Generic;
using plamp.Abstractions.Ast;

namespace plamp.Validators.BasicSemanticsValidators.MustReturn;

public class MustReturnValueInnerContext
{
    public required List<PlampException> Exceptions { get; init; } = [];

    public bool LexicalScopeAlwaysReturns = false;
    
    public required ISymbolTable SymbolTable { get; init; }
}