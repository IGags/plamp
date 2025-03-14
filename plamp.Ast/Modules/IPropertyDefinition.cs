using System.Diagnostics.CodeAnalysis;

namespace plamp.Ast.Modules;

public interface IPropertyDefinition : IWritableMember
{
    public string Name { get; }
    
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