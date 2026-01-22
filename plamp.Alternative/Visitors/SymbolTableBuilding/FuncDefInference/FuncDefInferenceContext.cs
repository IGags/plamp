using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Func;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.FuncDefInference;

public class FuncDefInferenceContext(SymbolTableBuildingContext other) : SymbolTableBuildingContext(other)
{
    public readonly List<FuncNode> ModuleFunctions = [];
}