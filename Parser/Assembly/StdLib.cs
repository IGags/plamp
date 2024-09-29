using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Parser.Assembly;

public class StdLib : IAssemblyDescription
{
    public string Name => "std";

    public IReadOnlyList<string> ReferenceAssemblies => [];

    private readonly List<KeyValuePair<Type, string>> _typeMap = new()
    {
        new(typeof(bool), "bool"),
        new(typeof(int), "int" ),
        new(typeof(long), "long" ),
        new(typeof(string), "string" ),
        new(typeof(DateTime), "date" ),
        new(typeof(TimeSpan), "timeSpan" ),
        new(typeof(double), "double" ),
        new(typeof(List<>), "List[]" ),
        new(typeof(void), "void" )
    };

    public IReadOnlyList<KeyValuePair<Type, string>> TypeMap => _typeMap;
    
    private readonly Dictionary<string, IReadOnlyList<MethodInfo>> _methodDescriptions = new()
    {
        {"toString", [typeof(int).GetMethod("ToString")]}
    };

    public IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> MethodDescriptions => _methodDescriptions;
    
    public bool TryMatchSignature(string name, Type returnType, List<Type> argumentTypes, out MethodInfo method)
    {
        throw new NotImplementedException();
    }
}