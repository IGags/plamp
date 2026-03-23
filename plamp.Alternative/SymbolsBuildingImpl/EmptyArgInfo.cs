using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class EmptyArgInfo(string name, ITypeInfo type) : IArgInfo
{
    public string Name { get; } = name;
    
    public ITypeInfo Type { get; } = type;
    
    public ParameterInfo AsInfo() => new ParameterInfoImpl(Type.AsType(), Name);
}