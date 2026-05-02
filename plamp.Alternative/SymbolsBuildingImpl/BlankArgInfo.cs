using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <inheritdoc/>
public class BlankArgInfo(string name, ITypeInfo type) : IArgInfo
{
    /// <inheritdoc/>
    public string Name { get; } = name;
    
    /// <inheritdoc/>
    public ITypeInfo Type { get; } = type;
    
    /// <inheritdoc/>
    public ParameterInfo AsInfo() => new ParameterInfoImpl(Type.AsType(), Name);
}