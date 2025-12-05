using System.Collections.Generic;
using plamp.Abstractions.Ast;

namespace plamp.Alternative.Visitors.ModulePreCreation.FuncDefInference;

public class FuncDefInferenceContext(PreCreationContext other) : PreCreationContext(other)
{
    public Dictionary<string, List<FilePosition>> Duplicates = [];
}