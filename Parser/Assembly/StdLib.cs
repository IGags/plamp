using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Parser.Assembly;

public class StdLib : IAssemblyDescription
{
    
    public static string ToString(int value)
    {
        return value.ToString();
    }

    public static string Concat(string left, string right)
    {
        return left + right;
    }
    
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
        {"WriteLine", new []{typeof(Console).GetMethod("WriteLine", [typeof(string)])}},
        {"ToString", new []{typeof(StdLib).GetMethod("ToString", [typeof(int)])}},
        {"Concat", new []{typeof(StdLib).GetMethod("Concat")}}
    };

    public IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> MethodDescriptions => _methodDescriptions;
    
    //TODO: Определяется в рантайме, что плохо
    public bool TryMatchSignature(string name, Type returnType, List<Type> argumentTypes, out MethodInfo method)
    {
        method = null;
        var overloads = (IEnumerable<MethodInfo>)_methodDescriptions[name];
        if (overloads == null)
        {
            return false;
        }

        if (returnType != null)
        {
            overloads = overloads.Where(x => x.ReturnType == returnType);
        }

        foreach (var overload in overloads)
        {
            var @params = overload.GetParameters().Select(x => x.ParameterType).ToList();
            if (argumentTypes.Count != @params.Count)
            {
                continue;
            }
            if (argumentTypes.Count == 0 && @params.Count == 0)
            {
                if (method != null)
                {
                    throw new InvalidOperationException();
                }
                method = overload;
            }

            var flag = true;
            for (int i = 0; i < argumentTypes.Count; i++)
            {
                if ((argumentTypes[i] == null && @params[i].IsClass) || argumentTypes[i] != @params[i])
                {
                    flag = false;
                    break;
                }
            }

            if (flag)
            {
                if (method != null)
                {
                    throw new InvalidOperationException();
                }
                method = overload;
            }
        }

        return method != null;
    }
}