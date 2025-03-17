using System.Collections.Generic;
using plamp.Ast.Node;

namespace plamp.Ast.Modules;

public interface ITypeDefinition
{
    public string Name { get; }
    
    public IReadOnlyList<IMethodDefinition> Methods { get; }
    
    public IReadOnlyList<IPropertyDefinition> Properties { get; }
    
    public IReadOnlyList<IFieldDefinition> Fields { get; }
    
    public bool TryGetMethod(
        string methodName, 
        out IMethodDefinition methodDefinition, 
        params ITypeDefinition[] args);
    
    public bool TryGetMethod(
        string methodName, 
        out IMethodDefinition methodDefinition, 
        params IArgDefinition[] args);
    
    public IReadOnlyList<IMethodDefinition> GetMethodOverloads(string methodName);
    
    public bool TryGetField(string name, out IFieldDefinition fieldDefinition);
    
    public bool TryGetProperty(string name, out IPropertyDefinition propertyDefinition);
}