using System;
using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Assemblies;

public interface IAssemblyContainer
{
    IReadOnlyList<ITypeInfo> GetMatchingTypes(string name, int genericsCount = 0, int arrayDimensionsCount = 0);
    
    IReadOnlyList<IFieldInfo> GetMatchingFields(string name, ITypeInfo enclosingType);

    IReadOnlyList<IPropertyInfo> GetMatchingProperties(string name, ITypeInfo enclosingType);
    
    IReadOnlyList<IMethodInfo> GetMatchingMethods(string name, ITypeInfo enclosingType, IReadOnlyList<ParameterInfo> signature = null);

    IReadOnlyList<IConstructorInfo> GetMatchingConstructors(ITypeInfo enclosingType, IReadOnlyList<ParameterInfo> signature = null);

    IReadOnlyList<IIndexerInfo> GetMatchingIndexers(ITypeInfo enclosingType, IReadOnlyList<ParameterInfo> signature = null);

    public IEnumerable<ITypeInfo> EnumerateTypes();
}