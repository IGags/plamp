using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Assemblies;
using plamp.Assembly.Models;

namespace plamp.Assembly;

internal class DefaultAssemblyContainer : IAssemblyContainer
{
    internal required Dictionary<string, List<DefaultTypeInfo>> Types { get; init; } = [];

    internal required Dictionary<ITypeInfo, List<DefaultFieldInfo>> Fields { get; init; } = [];

    internal required Dictionary<ITypeInfo, List<DefaultPropertyInfo>> Properties { get; init; } = [];

    internal required Dictionary<ITypeInfo, List<DefaultMethodInfo>> Methods { get; init; } = [];

    internal required Dictionary<ITypeInfo, List<DefaultConstructorInfo>> Constructors { get; init; } = [];

    internal required Dictionary<ITypeInfo, List<DefaultIndexerInfo>> Indexers { get; init; } = [];
        
    public IReadOnlyList<ITypeInfo> GetMatchingTypes(string name)
    {
        return Types.TryGetValue(name, out var list) ? list : [];
    }

    public IReadOnlyList<IFieldInfo> GetMatchingFields(string name, ITypeInfo enclosingType)
    {
        return Fields.TryGetValue(enclosingType, out var list) ? list.Where(x => x.Alias.Equals(name)).ToList() : [];
    }

    public IReadOnlyList<IPropertyInfo> GetMatchingProperties(string name, ITypeInfo enclosingType)
    {
        return Properties.TryGetValue(enclosingType, out var list) ? list.Where(x => x.Alias.Equals(name)).ToList() : [];
    }

    public IReadOnlyList<IMethodInfo> GetMatchingMethods(string name, ITypeInfo enclosingType, IReadOnlyList<ITypeInfo>? signature = null)
    {
        return Methods.TryGetValue(enclosingType, out var list)
            ? list.Where(x => x.Alias.Equals(name))
            .Where(x => signature == null || SignatureMatch(x.MethodInfo, signature))
            .ToList() : [];
    }

    public IReadOnlyList<IConstructorInfo> GetMatchingConstructors(ITypeInfo enclosingType, IReadOnlyList<ITypeInfo>? signature = null)
    {
        return Constructors.TryGetValue(enclosingType, out var list)
            ? list.Where(x => signature == null || SignatureMatch(x.ConstructorInfo, signature))
            .ToList() : [];
    }

    public IReadOnlyList<IIndexerInfo> GetMatchingIndexers(ITypeInfo enclosingType, IReadOnlyList<ITypeInfo>? signature = null)
    {
        if (!Indexers.TryGetValue(enclosingType, out var list)) return [];
        
        var getterMatches = list
            .Where(x => x.IndexerProperty.CanRead)
            .Where(x => signature == null || SignatureMatch(x.IndexerProperty.GetGetMethod()!, signature));
        var setterMatches = list.Where(x => x.IndexerProperty.CanWrite)
            .Where(x => x.IndexerProperty.CanWrite)
            .Where(x => signature == null || SignatureMatch(x.IndexerProperty.GetSetMethod()!, signature));
        return getterMatches.Concat(setterMatches).Distinct().ToList();
    }

    public IEnumerable<ITypeInfo> EnumerateTypes() => Types.Values.SelectMany(x => x);

    private bool SignatureMatch(MethodBase method, IReadOnlyList<ITypeInfo> signature)
    {
        var argumentTypes = method.GetParameters();
        var argPairs = argumentTypes.Zip(signature);
        return argPairs.All(x => ArgTypesMatch(x.First, x.Second));
    }

    private bool ArgTypesMatch(ParameterInfo parameter, ITypeInfo? typeInfo)
    {
        if (typeInfo == null 
            && (parameter.IsOptional 
                || !parameter.ParameterType.IsValueType 
                || parameter.HasDefaultValue)) return true;
        
        if (typeInfo == null) return false;
        if (parameter.ParameterType == typeInfo.Type) return true;
        if (parameter.ParameterType.IsGenericMethodParameter) return IsGenericConstraintsMatches(parameter, typeInfo);
        return typeInfo.Type.IsAssignableTo(parameter.ParameterType);
    }

    private bool IsGenericConstraintsMatches(ParameterInfo parameter, ITypeInfo typeInfo)
    {
        var type = typeInfo.Type;
        var constraints = parameter.ParameterType.GenericParameterAttributes;
        if ((constraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0 && type.IsValueType) return false;
        if ((constraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 && !type.IsValueType) return false;
        if ((constraints & GenericParameterAttributes.DefaultConstructorConstraint) != 0 && type.GetConstructor([]) == null) return false;
        var interfaces = parameter.ParameterType.GetInterfaces();
        foreach (var ifs in interfaces)
        {
            if (!type.IsAssignableTo(ifs)) return false;
        }

        var baseType = parameter.ParameterType.BaseType;
        if (baseType == null) return true;
        return type.IsAssignableTo(baseType);
    }
}