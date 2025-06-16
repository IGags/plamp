using System;

namespace plamp.Abstractions.Compilation.Models;

public readonly record struct SourceFile(string FileName, string SourceCode)
{
    private int StringHashCode { get; } = SourceCode.GetHashCode();
    
    public override int GetHashCode() => HashCode.Combine(FileName, StringHashCode, GetType());
}