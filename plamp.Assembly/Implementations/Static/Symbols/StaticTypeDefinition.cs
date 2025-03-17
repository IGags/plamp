using System.Collections.Generic;
using plamp.Ast.Modules;

namespace plamp.Assembly.Implementations.Static.Symbols;

public class StaticTypeDefinition : ITypeDefinition
{
    public string Name { get; }
    public IReadOnlyList<IMethodDefinition> Methods { get; }
    public bool TryGetMethod(string methodName, out IMethodDefinition methodDefinition, params ITypeDefinition[] args)
    {
        throw new System.NotImplementedException();
    }

    public bool TryGetMethod(string methodName, out IMethodDefinition methodDefinition, params IArgDefinition[] args)
    {
        throw new System.NotImplementedException();
    }

    public IReadOnlyList<IMethodDefinition> GetMethodOverloads(string methodName)
    {
        throw new System.NotImplementedException();
    }
}