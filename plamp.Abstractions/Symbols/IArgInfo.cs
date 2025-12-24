namespace plamp.Abstractions.Symbols;

public interface IArgInfo
{
    public string Name { get; }
    
    public ITypeInfo Type { get; }
}