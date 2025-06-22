using System;
using System.Reflection;

namespace plamp.Abstractions.Assemblies;

public interface IAssemblyContainer
{
    Type[] GetMatchingTypes(string name);
    
    FieldInfo[] GetMatchingFields(string name, Type enclosingType);

    PropertyInfo[] GetMatchingProperties(string name, Type enclosingType);
    
    MethodInfo[] GetMatchingMethods(string name, Type enclosingType, Type[] signature);

    ConstructorInfo[] GetMatchingConstructors(string name, Type enclosingType, Type[] signature);
}