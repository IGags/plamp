using System.Collections.Generic;
using System.Linq;
using plamp.Assembly.Implementations.Dynamic.Symbols;
using plamp.Ast;
using plamp.Ast.Modules;

namespace plamp.Assembly.Implementations.Dynamic;

internal class DynamicModuleDefinition : IModuleDefinition
{
    private readonly List<DynamicTypeDefinition> _types = [];

    private readonly List<IModuleDefinition> _modules = [];
    
    private IMethodDefinition _entryPointMethod;
    
    public string Name { get; }

    public IReadOnlyList<ITypeDefinition> Types => _types;

    public IReadOnlyList<IModuleDefinition> Dependencies => _modules;

    public DynamicModuleDefinition(string name)
    {
        Name = name;
    }

    public ITypeDefinition FindType(string name) 
        => _types.FirstOrDefault(x => x.Name.Equals(name));

    public IModuleDefinition FindDependency(string name)
        => _modules.FirstOrDefault(x => x.Name.Equals(name));

    public bool TryGetEntryPointMethod(out IMethodDefinition methodDefinition)
    {
        methodDefinition = _entryPointMethod;
        return _entryPointMethod != null;
    }

    public bool TryAddType(TypeFullName fullName)
    {
        if (_types.Select(x => x.Name).Contains(fullName.TypeName))
        {
            return false;
        }

        var type = new DynamicTypeDefinition(fullName.TypeName);
        _types.Add(type);
        return true;
    }
}