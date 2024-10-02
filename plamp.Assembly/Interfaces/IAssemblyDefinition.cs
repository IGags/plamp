using System;
using System.Collections.Generic;
using System.Reflection;

namespace plamp.Assembly.Interfaces;

public interface IAssemblyDefinition
{
    public string Name { get; }
    public IReadOnlyDictionary<string, Type> TypeDictionary { get; }
    public IReadOnlyList<MethodInfo> GetMethodList { get; }
    public IReadOnlyList<ParameterInfo> GetIndexerList { get; }
}