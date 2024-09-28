using System;
using System.Collections.Generic;
using System.Reflection;

namespace Parser.Assembly;

/// <summary>
/// Описание сборки
/// </summary>
public interface IAssemblyDescription
{
    public string Name { get; }
    
    public IReadOnlyList<string> ReferenceAssemblies { get; }
    
    public IReadOnlyList<KeyValuePair<Type, string>> TypeMap { get; }
    
    public IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> MethodDescriptions { get; }

    public bool TryMatchSignature(string name, Type returnType, List<Type> argumentTypes, out MethodInfo method);
}