using plamp.Ast.Modules;

namespace plamp.Assembly.Implementations.Static.Symbols;

public class StaticMethodDefinition : IMethodDefinition
{
    public string Name { get; }
    public ITypeDefinition ReturnType { get; }
    public IArgDefinition[] Arguments { get; }
}