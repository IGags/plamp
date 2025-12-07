using System.Collections.Generic;
using plamp.Abstractions.Ast;

namespace plamp.Alternative.Visitors.ModulePreCreation.FuncDefInference;

public class FuncDefInferenceContext(SymbolTableBuildingContext other) : SymbolTableBuildingContext(other)
{
    public Dictionary<string, List<FilePosition>> Duplicates = [];
}