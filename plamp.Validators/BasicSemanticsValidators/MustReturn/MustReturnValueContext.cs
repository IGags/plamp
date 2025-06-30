using System.Collections.Generic;
using plamp.Abstractions.Ast;

namespace plamp.Validators.BasicSemanticsValidators.MustReturn;

public record MustReturnValueContext
{
    public required ISymbolTable SymbolTable { get; init; }

    public List<PlampException> Exceptions { get; init; } = [];

}