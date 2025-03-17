using plamp.Ast.Modules;

namespace plamp.Assembly.Implementations.Static.Symbols;

public class StaticArgDefinition : IArgDefinition
{
    public string Name { get; }
    public ITypeDefinition Type { get; }
}