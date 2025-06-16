using System;

namespace plamp.Assembly.Impl.BuiltRecords;

public readonly record struct PreparedType(Type Type, string Namespace, string Alias, PreparedMember[] Members);