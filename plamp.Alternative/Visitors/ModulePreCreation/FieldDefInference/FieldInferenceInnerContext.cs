using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

namespace plamp.Alternative.Visitors.ModulePreCreation.FieldDefInference;

public class FieldInferenceInnerContext(SymbolTableBuildingContext other) : SymbolTableBuildingContext(other)
{
    public Dictionary<string, List<FieldNameNode>> Duplicates { get; } = [];
}