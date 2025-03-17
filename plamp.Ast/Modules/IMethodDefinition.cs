namespace plamp.Ast.Modules;

public interface IMethodDefinition
{
    /// <summary>
    /// Name of method
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Return type
    /// </summary>
    public ITypeDefinition ReturnType { get; }
    
    /// <summary>
    /// Method args
    /// </summary>
    public IArgDefinition[] Arguments { get; }
}