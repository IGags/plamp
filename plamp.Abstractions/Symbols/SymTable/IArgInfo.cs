using System.Reflection;

namespace plamp.Abstractions.Symbols.SymTable;

public interface IArgInfo
{
    public string Name { get; }
    
    public ITypeInfo Type { get; }

    public ParameterInfo AsInfo();
}