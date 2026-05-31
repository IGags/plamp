using System.Collections.Generic;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.FuncDefInference;

public class FuncDefInferenceContext(SymbolTableBuildingContext other) : SymbolTableBuildingContext(other)
{
    public HashSet<string> DuplicateNames { get; set; } = [];
    
    //У дженериков переопределены Equals и GetHashCode
    public HashSet<IGenericParameterBuilder> CurrentFuncGenerics { get; } = [];
}