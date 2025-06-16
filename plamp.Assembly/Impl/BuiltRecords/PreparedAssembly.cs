using System.Reflection;

namespace plamp.Assembly.Impl.BuiltRecords;

public readonly record struct PreparedAssembly(System.Reflection.Assembly Assembly, AssemblyName Alias, PreparedType[] Types);