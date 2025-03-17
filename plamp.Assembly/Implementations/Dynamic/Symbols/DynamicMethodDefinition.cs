using plamp.Ast.Modules;

namespace plamp.Assembly.Implementations.Dynamic.Symbols;

internal class DynamicMethodDefinition : IMethodDefinition
{
    public string Name { get; }
    
    public ITypeDefinition ReturnType { get; }

    public IArgDefinition[] Arguments { get; }

    public DynamicMethodDefinition(string name, ITypeDefinition returnType, IArgDefinition[] arguments)
    {
        Name = name;
        ReturnType = returnType;
        Arguments = arguments;
    }
}