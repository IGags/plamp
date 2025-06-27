using System;
using System.Collections.Frozen;
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
        
    public IReadOnlyList<ITypeInfo> GetMatchingTypes(string name, int genericsCount = 0, int arrayDimensionsCount = 0)
    {
        var types = Types.TryGetValue(name, out var list) 
            ? list.Where(x => x.Type.GetGenericArguments().Length == genericsCount).ToList() 
            : [];
        if (types.Count == 0 && DefaultPrivateCoreLibContent.GetCoreTypeIfExists(name, out var info))
        {
            types = [info!];
        }

        if (arrayDimensionsCount == 0) return types;
        
        var arrays = new List<DefaultTypeInfo>();
        foreach (var type in types)
        {
            if (DefaultPrivateCoreLibContent.TryGetArrayType(type, arrayDimensionsCount, out var arrayType))
            {
                arrays.Add(arrayType!);
            }
        }

        return arrays;
    }

    public IReadOnlyList<IFieldInfo> GetMatchingFields(string name, ITypeInfo enclosingType)
    {
        return Fields.TryGetValue(enclosingType, out var list) ? list.Where(x => x.Alias.Equals(name)).ToList() : [];
    }

    public IReadOnlyList<IPropertyInfo> GetMatchingProperties(string name, ITypeInfo enclosingType)
    {
        return Properties.TryGetValue(enclosingType, out var list) ? list.Where(x => x.Alias.Equals(name)).ToList() : [];
    }

    public IReadOnlyList<IMethodInfo> GetMatchingMethods(string name, ITypeInfo enclosingType, IReadOnlyList<ParameterInfo>? signature = null)
    {
        return Methods.TryGetValue(enclosingType, out var list)
            ? list.Where(x => x.Alias.Equals(name))
            .Where(x => signature == null || SignatureMatch(x.MethodInfo, signature))
            .ToList() : [];
    }

    public IReadOnlyList<IConstructorInfo> GetMatchingConstructors(ITypeInfo enclosingType, IReadOnlyList<ParameterInfo>? signature = null)
    {
        return Constructors.TryGetValue(enclosingType, out var list)
            ? list.Where(x => signature == null || SignatureMatch(x.ConstructorInfo, signature))
            .ToList() : [];
    }

    public IReadOnlyList<IIndexerInfo> GetMatchingIndexers(ITypeInfo enclosingType, IReadOnlyList<ParameterInfo>? signature = null)
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

    private bool SignatureMatch(MethodBase method, IReadOnlyList<ParameterInfo> signature)
    {
        var argumentTypes = method.GetParameters();
        var argPairs = argumentTypes.Zip(signature);
        return argPairs.All(x => ArgTypesMatch(x.First, x.Second));
    }

    private bool ArgTypesMatch(ParameterInfo parameter, ParameterInfo? typeInfo)
    {
        if (typeInfo == null 
            && (parameter.IsOptional 
                || !parameter.ParameterType.IsValueType 
                || parameter.HasDefaultValue)) return true;
        
        if (typeInfo == null) return false;
        if (parameter.ParameterType == typeInfo.ParameterType) return true;
        if (parameter.ParameterType.IsGenericMethodParameter) return IsGenericConstraintsMatches(parameter, typeInfo);
        if (parameter.ParameterType.IsGenericTypeParameter) return IsGenericConstraintsMatches(parameter, typeInfo);
        return typeInfo.ParameterType.IsAssignableTo(parameter.ParameterType);
    }

    private bool IsGenericConstraintsMatches(ParameterInfo parameter, ParameterInfo typeInfo)
    {
        var type = typeInfo.ParameterType;
        var constraints = parameter.ParameterType.GenericParameterAttributes;
        if ((constraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0 && type.IsValueType) return false;
        if ((constraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 && !type.IsValueType) return false;
        if ((constraints & GenericParameterAttributes.DefaultConstructorConstraint) != 0 && !type.IsValueType && type.GetConstructor([]) == null) return false;
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

//TODO: naming overrides
internal static class DefaultPrivateCoreLibContent
{
    /// <summary>
    /// Here live basic types such as strings, ints, arrays...
    /// Lib must not be explicitly imported in code files cause name can be changed
    /// </summary>
    public const string BasicTypeModuleName = "$runtime-private-lib";

    private static readonly FrozenDictionary<string, Type> DefaultTypeDict = new Dictionary<string, Type>()
    {
        { "int", typeof(int) },
        { "short", typeof(short) },
        { "uint", typeof(uint) },
        { "ushort", typeof(ushort) },
        { "long", typeof(long) },
        { "ulong", typeof(ulong) },
        { "byte", typeof(byte) },
        { "sbyte", typeof(sbyte) },
        { "bool", typeof(bool) },
        { "char", typeof(char) },
        { "string", typeof(string) },
        { "float", typeof(float) },
        { "double", typeof(double) },
    }.ToFrozenDictionary();

    // private static readonly HashSet<string> RestrictedArrayMethodsList =
    // [
    //     nameof(Array.AsReadOnly),
    //     nameof(Array.BinarySearch),
    //     nameof(Array.ConvertAll),
    //     nameof(Array.Exists),
    //     nameof(Array.Find),
    //     nameof(Array.FindAll),
    //     nameof(Array.FindIndex),
    //     nameof(Array.FindLast),
    //     nameof(Array.FindLastIndex),
    //     nameof(Array.ForEach),
    //     nameof(Array.GetEnumerator),
    //     nameof(Array.TrueForAll)
    // ];
    //
    // private static readonly FrozenDictionary<string, List<MethodInfo>> ArrayBaseMethods =
    //     typeof(Array).GetMethods().GroupBy(x => x.Name)
    //         .Where(x => !RestrictedArrayMethodsList.Contains(x.Key))
    //         .ToFrozenDictionary(x => x.Key, x => x.ToList());
    
    public static bool GetCoreTypeIfExists(string name, out DefaultTypeInfo? typeInfo)
    {
        if (!DefaultTypeDict.TryGetValue(name, out var type))
        {
            typeInfo = null;
            return false;
        }

        typeInfo = new DefaultTypeInfo(name, type, BasicTypeModuleName);
        return true;
    }

    public static bool TryGetArrayType(ITypeInfo typeInfo, int dimensionsCount, out DefaultTypeInfo? arrayType)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(dimensionsCount, 1, nameof(dimensionsCount));
        if (typeInfo.Type.IsGenericTypeDefinition)
        {
            arrayType = null;
            return false;
        }

        var type = typeInfo.Type.MakeArrayType(dimensionsCount);
        arrayType = new DefaultTypeInfo(typeInfo.Alias, type, BasicTypeModuleName);
        return true;
    }
}