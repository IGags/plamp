using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.Symbols.SymTableBuilding;

public interface IGenericParameterBuilder : ITypeInfo
{
    public GenericTypeParameterBuilder? TypeBuilder { get; set; }
}