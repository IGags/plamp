using System;

namespace plamp.Abstractions.Compilation.Models;

public record struct SourceFile(string FileName, string SourceCode)
{
    private int? _stringHashCode;

    private int StringHashCode
    {
        get
        {
            _stringHashCode ??= SourceCode.GetHashCode();
            return _stringHashCode.Value;
        }
    }
    
    public override int GetHashCode() => HashCode.Combine(FileName, StringHashCode, GetType());
}