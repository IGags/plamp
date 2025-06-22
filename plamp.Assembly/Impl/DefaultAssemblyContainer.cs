using System;
using System.Reflection;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Impl;

internal class DefaultAssemblyContainer : IAssemblyContainer
{
    public Type[] GetMatchingTypes(string name)
    {
        throw new NotImplementedException();
    }

    public FieldInfo[] GetMatchingFields(string name, Type enclosingType)
    {
        throw new NotImplementedException();
    }

    public PropertyInfo[] GetMatchingProperties(string name, Type enclosingType)
    {
        throw new NotImplementedException();
    }

    public MethodInfo[] GetMatchingMethods(string name, Type enclosingType, Type[] signature)
    {
        throw new NotImplementedException();
    }

    public ConstructorInfo[] GetMatchingConstructors(string name, Type enclosingType, Type[] signature)
    {
        throw new NotImplementedException();
    }
}