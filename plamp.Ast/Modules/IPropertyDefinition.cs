using System.Diagnostics.CodeAnalysis;

namespace plamp.Ast.Modules;

public interface IPropertyDefinition
{
    /// <summary>
    /// Name of property
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Property type
    /// </summary>
    public ITypeDefinition Type { get; }
    
    /// <summary>
    /// Setter allowed to be null. Then compiler should generate default methods
    /// </summary>
    [AllowNull]
    public IMethodDefinition SetMethod { get; }
    
    /// <summary>
    /// Getter allowed to be null. Then compiler should generate default methods
    /// </summary>
    [AllowNull]
    public IMethodDefinition GetMethod { get; }
}