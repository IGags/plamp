using plamp.Abstractions.Symbols;

namespace plamp.Alternative.SymbolsImpl;

public class ArgInfo(string name, ITypeInfo typeInfo) : IArgInfo
{
    public string Name => name;

    public ITypeInfo Type => typeInfo;
}