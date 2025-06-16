using System.Reflection;
using plamp.Assembly.Impl.Builders;

namespace plamp.Assembly.Impl.Models;

internal readonly record struct AssemblyBuilderInfo(
    StaticAssemblyBuilder AssemblyBuilder,
    AssemblyName Alias);