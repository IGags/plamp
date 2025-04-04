namespace plamp.Assembly.Impl.BuiltRecords;

public readonly record struct PreparedAssembly(System.Reflection.Assembly Assembly, string Alias, PreparedType[] Types);