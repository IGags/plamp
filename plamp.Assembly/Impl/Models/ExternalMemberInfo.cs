using System.Reflection;

namespace plamp.Assembly.Impl.Models;

internal readonly record struct ExternalMemberInfo(MemberInfo Member, string Alias);