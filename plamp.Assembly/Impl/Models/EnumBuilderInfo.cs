using plamp.Assembly.Impl.Builders;

namespace plamp.Assembly.Impl.Models;

internal readonly record struct EnumBuilderInfo(
    StaticEnumBuilder TypeBuilder,
    string Alias,
    string NamespaceOverride);