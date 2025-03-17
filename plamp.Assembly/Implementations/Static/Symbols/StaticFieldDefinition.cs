using plamp.Ast.Modules;

namespace plamp.Assembly.Implementations.Static.Symbols;

public class StaticFieldDefinition : IFieldDefinition
{
    public string Name { get; }
    public ITypeDefinition Type { get; }
}