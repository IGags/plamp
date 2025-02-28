using System.Collections.Generic;
using plamp.Ast;

namespace plamp.Validators.Models;

public record ValidationResult
{
    public List<PlampException> Exceptions { get; init; } = [];
}