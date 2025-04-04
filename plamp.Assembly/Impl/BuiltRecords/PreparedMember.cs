using System.Reflection;

namespace plamp.Assembly.Impl.BuiltRecords;

public readonly record struct PreparedMember(MemberInfo Info, string Alias);