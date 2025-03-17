namespace plamp.Ast.Modules;

public interface IFieldDefinition
{
    public string Name { get; }
    
    public ITypeDefinition Type { get; }
}