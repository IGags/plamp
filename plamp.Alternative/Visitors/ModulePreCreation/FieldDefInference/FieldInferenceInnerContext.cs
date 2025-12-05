using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

namespace plamp.Alternative.Visitors.ModulePreCreation.FieldDefInference;

public class FieldInferenceInnerContext(PreCreationContext other) : PreCreationContext(other)
{
    public Dictionary<string, List<FieldNameNode>> Duplicates { get; } = [];
}