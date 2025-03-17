using plamp.Ast.Modules;

namespace plamp.Assembly.Implementations.Dynamic.Symbols;

internal class DynamicPropertyDefinition : IPropertyDefinition
{
    public string Name { get; }
    
    public ITypeDefinition Type { get; }

    public IMethodDefinition SetMethod => null;

    public IMethodDefinition GetMethod => null;

    public DynamicPropertyDefinition(string name, ITypeDefinition type)
    {
        Name = name;
        Type = type;
    }
}