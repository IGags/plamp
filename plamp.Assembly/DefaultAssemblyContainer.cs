using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Assemblies;
using plamp.Assembly.Models;

namespace plamp.Assembly;

internal class DefaultAssemblyContainer : IAssemblyContainer
{
    internal Dictionary<string, List<DefaultTypeInfo>> Types { get; } = [];

    internal Dictionary<ITypeInfo, List<DefaultFieldInfo>> Fields { get; } = [];

    internal Dictionary<ITypeInfo, List<DefaultPropertyInfo>> Properties { get; } = [];

    internal Dictionary<ITypeInfo, List<DefaultMethodInfo>> Methods { get; } = [];

    internal Dictionary<ITypeInfo, List<DefaultConstructorInfo>> Constructors { get; } = [];
        
    public IReadOnlyList<ITypeInfo> GetMatchingTypes(string name)
    {
        return Types[name] ?? [];
    }

    public IReadOnlyList<IFieldInfo> GetMatchingFields(string name, ITypeInfo enclosingType)
    {
        return Fields[enclosingType]?.Where(x => x.Alias.Equals(name)).ToList() ?? [];
    }

    public IReadOnlyList<IPropertyInfo> GetMatchingProperties(string name, ITypeInfo enclosingType)
    {
        return Properties[enclosingType]?.Where(x => x.Alias.Equals(name)).ToList() ?? [];
    }

    public IReadOnlyList<IMethodInfo> GetMatchingMethods(string name, ITypeInfo enclosingType, IReadOnlyList<ITypeInfo> signature)
    {
        return Methods[enclosingType]?
            .Where(x => x.Alias.Equals(name))
            .Where(x => SignatureMatch(x.MethodInfo, signature))
            .ToList() ?? [];
    }

    public IReadOnlyList<IConstructorInfo> GetMatchingConstructors(string name, ITypeInfo enclosingType, IReadOnlyList<ITypeInfo> signature)
    {
        return Constructors[enclosingType]?
            .Where(x => x.EnclosingType.Alias.Equals(name))
            .Where(x => SignatureMatch(x.ConstructorInfo, signature))
            .ToList() ?? [];
    }

    private bool SignatureMatch(MethodBase method, IReadOnlyList<ITypeInfo> signature)
    {
        var argumentTypes = method.GetParameters();
        var argPairs = argumentTypes.Zip(signature);
        return argPairs.All(x => ArgTypesMatch(x.First, x.Second));
    }

    private bool ArgTypesMatch(ParameterInfo parameter, ITypeInfo typeInfo)
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