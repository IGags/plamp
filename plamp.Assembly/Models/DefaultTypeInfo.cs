using System;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

internal class DefaultTypeInfo : ITypeInfo
{
    public required Type Type { get; init; }
    public string? Alias { get; set; }
    public required string Module { get; init; }
}