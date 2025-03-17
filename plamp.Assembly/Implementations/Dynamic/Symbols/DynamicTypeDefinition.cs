using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Ast.Modules;

namespace plamp.Assembly.Implementations.Dynamic.Symbols;

internal class DynamicTypeDefinition : ITypeDefinition
{
    //TODO: hashset
    private readonly List<DynamicMethodDefinition> _methods = [];
    
    private readonly List<DynamicPropertyDefinition> _properties = [];
    
    public string Name { get; }
    
    public IReadOnlyList<IMethodDefinition> Methods => _methods;
    
    public IReadOnlyList<IPropertyDefinition> Properties => _properties;

    public IReadOnlyList<IFieldDefinition> Fields => throw new NotImplementedException();

    public DynamicTypeDefinition(string name)
    {
        Name = name;
    }
    
    public bool TryGetMethod(string methodName, out IMethodDefinition methodDefinition, params ITypeDefinition[] args)
    {
        methodDefinition = _methods
            .Where(x => x.Name.Equals(methodName))
            .SingleOrDefault(x => 
                x.Arguments.Zip(args).All(y => y.First.Type.Equals(y.Second)));
        return methodDefinition != null;
    }

    public bool TryGetMethod(string methodName, out IMethodDefinition methodDefinition, params IArgDefinition[] args)
    {
        methodDefinition = _methods
            .Where(x => x.Name.Equals(methodName))
            .SingleOrDefault(x => 
                x.Arguments.Zip(args).All(y => y.First.Equals(y.Second)));
        return methodDefinition != null;
    }

    public IReadOnlyList<IMethodDefinition> GetMethodOverloads(string methodName)
        => _methods.Where(x => x.Name.Equals(methodName)).ToList();

    public bool TryGetField(string name, out IFieldDefinition fieldDefinition)
        => throw new NotImplementedException();

    public bool TryGetProperty(string name, out IPropertyDefinition propertyDefinition)
    {
        throw new NotImplementedException();
    }

    public bool TryAddMethod(string name, ITypeDefinition returnType, params IArgDefinition[] args)
    {
        if (!TryGetMethod(name, out _, args))
        {
            return false;
        }
        
        var methodDefinition = new DynamicMethodDefinition(name, returnType, args);
        _methods.Add(methodDefinition);
        return true;
    }

    public bool TryAddProperty(string name, ITypeDefinition returnType)
    {
        if (!TryGetProperty(name, out _))
        {
            return false;
        }
        
        var prop = new DynamicPropertyDefinition(name, returnType);
        _properties.Add(prop);
        return true;
    }
}