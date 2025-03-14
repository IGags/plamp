using System.Collections.Generic;
using plamp.Ast.Node;

namespace plamp.Ast.Modules;

public interface ITypeDefinition : IWritableMember
{
    public string Name { get; }
    
    public IReadOnlyList<IMethodDefinition> Methods { get; }
    
    public IMethodDefinition GetMethod(string name);
}