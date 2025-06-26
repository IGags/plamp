using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Assemblies;

public interface IAssemblyContainer
{
    IReadOnlyList<ITypeInfo> GetMatchingTypes(string name);
    
    IReadOnlyList<IFieldInfo> GetMatchingFields(string name, ITypeInfo enclosingType);

    IReadOnlyList<IPropertyInfo> GetMatchingProperties(string name, ITypeInfo enclosingType);
    
    IReadOnlyList<IMethodInfo> GetMatchingMethods(string name, ITypeInfo enclosingType, IReadOnlyList<ITypeInfo> signature = null);

    IReadOnlyList<IConstructorInfo> GetMatchingConstructors(ITypeInfo enclosingType, IReadOnlyList<ITypeInfo> signature = null);

    IReadOnlyList<IIndexerInfo> GetMatchingIndexers(ITypeInfo enclosingType, IReadOnlyList<ITypeInfo> signature = null);

    public IEnumerable<ITypeInfo> EnumerateTypes();
}