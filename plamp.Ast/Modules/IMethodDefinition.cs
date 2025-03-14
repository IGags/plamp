namespace plamp.Ast.Modules;

public interface IMethodDefinition : IWritableMember
{
    public string Name { get; }
    
    public ITypeDefinition ReturnType { get; }
    
    public ITypeDefinition[] Arguments { get; }
}