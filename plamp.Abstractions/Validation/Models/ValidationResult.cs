using System.Collections.Generic;
using plamp.Abstractions.Ast;

namespace plamp.Abstractions.Validation.Models;

public record ValidationResult
{
    public required List<PlampException> Exceptions { get; init; }
}