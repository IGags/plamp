using plamp.Ast.Modules;

namespace plamp.Assembly.Implementations.Dynamic.Symbols;

public class DynamicArgDefinition : IArgDefinition
{
    public string Name { get; }
    public ITypeDefinition Type { get; }
}