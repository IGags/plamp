using plamp.Assembly.Impl.Builders;

namespace plamp.Assembly.Impl.Models;

internal readonly record struct TypeBuilderInfo(
    StaticTypeBuilder TypeBuilder,
    string Alias,
    string NamespaceOverride);