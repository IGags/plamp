using System.Reflection;

namespace plamp.Assembly.Impl.Models;

internal readonly record struct EnumFieldInfo(FieldInfo Field, string Alias);