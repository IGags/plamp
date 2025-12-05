using System.Collections.Generic;
using plamp.Abstractions.Ast;

namespace plamp.Alternative.Visitors.ModulePreCreation.TypedefInference;

public class TypedefInferenceVisitorContext(PreCreationContext other) : PreCreationContext(other)
{
    public Dictionary<string, List<FilePosition>> Duplicates { get; } = [];
}